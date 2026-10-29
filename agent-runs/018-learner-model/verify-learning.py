"""Local gateway acceptance for Epic 2; creates disposable collections only.
Run from workspace: python3 LearnWord/agent-runs/018-learner-model/verify-learning.py
Account helpers reuse the accepted Social Collections gateway/mail flow.
"""
import importlib.util
from concurrent.futures import ThreadPoolExecutor
import json
from pathlib import Path
import uuid

helper_path = Path(__file__).resolve().parents[1] / '014-social-collections' / 'verify-social.py'
spec = importlib.util.spec_from_file_location('social_acceptance', helper_path)
helper = importlib.util.module_from_spec(spec)
spec.loader.exec_module(helper)
request, account = helper.request, helper.account


def run():
    owner = account('learner-owner-local@example.com')
    visitor = account('learner-visitor-local@example.com')
    cleanup = []
    checks = []

    def passed(name):
        checks.append(name)
        print('PASS:', name, flush=True)

    def collection(token, label):
        result = request('POST', '/api/collections', {'name': 'Learner acceptance ' + label + ' ' + uuid.uuid4().hex[:8]}, token, 201)
        cleanup.append((token, result['id']))
        return result

    def card(token, cid, value):
        return request('POST', '/api/cards', {'collectionId': cid, 'words': [
            {'value': value, 'transcription': '[' + value + ']', 'translation': 'перевод ' + value}]}, token, 201)

    try:
        source = collection(owner, 'source')
        cid = source['id']
        base = '/api/learning/collections/' + str(cid)
        first = card(owner, cid, 'river')
        second = card(owner, cid, 'stream')
        third = card(owner, cid, 'lake')
        for suffix in ('/today', '/summary'):
            request('GET', base + suffix, expected=401)
            request('GET', base + suffix, token=visitor, expected=404)
        request('POST', base + '/sessions', {}, expected=401)
        request('POST', base + '/sessions', {}, visitor, 404)
        initial = request('GET', base + '/summary', token=owner)
        assert initial['new'] == 3 and initial['attemptsCount'] == 0, initial
        plan = request('GET', base + '/today', token=owner)
        assert plan['totalCount'] == 3 and plan['groups']['new'] == 3, plan
        assert len({c['id'] for c in plan['cards']}) == 3
        assert request('GET', base + '/summary', token=owner) == initial
        for endpoint in ('/state', '/attempts'):
            request('GET', '/api/learning/cards/' + str(first['id']) + endpoint, expected=401)
            request('GET', '/api/learning/cards/' + str(first['id']) + endpoint, token=visitor, expected=403)
        passed('Authenticated learner reads, foreign collection/card isolation, side-effect-free fresh recommendation')

        session = request('POST', base + '/sessions', {}, owner)
        sid = session['id']
        assert session['plannedCardsCount'] == 3 and session['completedCardsCount'] == 0, session
        complete = '/api/learning/sessions/' + sid + '/complete'
        request('POST', complete, {}, expected=401)
        request('POST', complete, {}, visitor, 404)
        for bad in (-1, 600001):
            request('POST', '/api/review/cards/' + str(first['id']) + '/review',
                    {'outcome': 'Good', 'responseTimeMs': bad, 'sessionId': sid}, owner, 400)
        unchanged = request('GET', base + '/summary', token=owner)
        assert unchanged['attemptsCount'] == 0, unchanged
        request('POST', '/api/review/cards/' + str(first['id']) + '/review',
                {'outcome': 'Good', 'sessionId': sid}, visitor, 403)
        foreign = collection(visitor, 'foreign')
        foreign_card = card(visitor, foreign['id'], 'foreign')
        request('POST', '/api/review/cards/' + str(foreign_card['id']) + '/review',
                {'outcome': 'Good', 'sessionId': sid}, visitor, 404)
        passed('Session authorization, invalid telemetry and failed reviews leave history unchanged')

        review_path = '/api/review/cards/' + str(first['id']) + '/review'
        request_id = str(uuid.uuid4())
        body = {'outcome': 'Again', 'sessionId': sid, 'responseTimeMs': 1200, 'requestId': request_id}
        # Concurrent retries exercise the real PostgreSQL row lock, beyond sequential unit checks.
        with ThreadPoolExecutor(max_workers=3) as pool:
            replies = list(pool.map(lambda _: request('POST', review_path, body, owner), range(3)))
        again = replies[0]
        assert all(reply['reviewCount'] == 1 for reply in replies), replies
        assert again['intervalDays'] == 1 and not again['learnt']
        assert request('GET', base + '/summary', token=owner)['attemptsCount'] == 1
        for current, outcome in ((second, 'Hard'), (third, 'Easy')):
            request('POST', '/api/review/cards/' + str(current['id']) + '/review',
                    {'outcome': outcome, 'sessionId': sid, 'requestId': str(uuid.uuid4())}, owner)
        done = request('POST', complete, {}, owner)
        done_twice = request('POST', complete, {}, owner)
        assert done['completedCardsCount'] == 3 and done['failedReviews'] == 1, done
        assert done['successfulReviews'] == 2 and done['completedAt'], done
        assert done_twice == done
        passed('Atomic session counters, stable review retry, Again/Hard/Easy outcomes and idempotent completion')

        # Existing clients remain valid; deterministic scheduler advances as before.
        for outcome, interval in (('Good', 3), ('Good', 7), ('Easy', 21)):
            result = request('POST', review_path, {'outcome': outcome}, owner)
            assert result['intervalDays'] == interval, (outcome, result)
        assert result['reviewCount'] == 4 and result['learnt']
        history = request('GET', '/api/learning/cards/' + str(first['id']) + '/attempts', token=owner)
        assert history['totalCount'] == 4, history
        observed = sorted(history['items'], key=lambda a: a['reviewedAt'])
        assert [a['outcome'] for a in observed] == ['Again', 'Good', 'Good', 'Easy'], observed
        assert [a['newIntervalDays'] for a in observed] == [1, 3, 7, 21]
        assert observed[0]['responseTimeMs'] == 1200 and observed[0]['sessionId'] == sid
        for previous, following in zip(observed, observed[1:]):
            assert previous['newIntervalDays'] == following['previousIntervalDays']
            assert previous['newDueDate'] == following['previousDueDate']
        state = request('GET', '/api/learning/cards/' + str(first['id']) + '/state', token=owner)
        assert state['attemptsCount'] == 4 and state['failedAttempts'] == 1, state
        assert state['successfulAttempts'] == 3 and 0 <= state['masteryScore'] <= 1, state
        summary = request('GET', base + '/summary', token=owner)
        assert summary['attemptsCount'] == 6 and summary['totalCardsStudied'] == 3, summary
        assert sum(summary[k] for k in ('new', 'learning', 'weak', 'strong')) == 3
        assert summary['new'] == 0
        changed = request('GET', base + '/today', token=owner)
        assert changed['groups']['new'] == 0
        assert len({c['id'] for c in changed['cards']}) == changed['totalCount']
        passed('Again/Good/Good/Easy backward-compatible scheduling and changed learner recommendation')

        request('PUT', '/api/collections/' + str(cid) + '/visibility', {'visibility': 2}, owner)
        shared_path = '/api/social/collections/' + source['publicId']
        shared = request('GET', shared_path + '?search=river', token=visitor)
        assert shared['totalCount'] == 1
        assert set(shared['cards'][0]) == {'words'}, shared['cards'][0]
        assert all(set(w) == {'value', 'transcription', 'translation'} for w in shared['cards'][0]['words'])
        for key in ('masteryScore', 'difficultyScore', 'attemptsCount', 'lastAttemptAt', 'sessionId', 'responseTimeMs', 'learnerCardState'):
            assert key not in json.dumps(shared), key
        fork = request('POST', shared_path + '/fork', token=visitor, expected=201)
        cleanup.append((visitor, fork['id']))
        assert fork['visibility'] == 0 and len(fork['cards']) == 3
        assert all(c['reviewCount'] == 0 and not c['learnt'] for c in fork['cards'])
        fork_base = '/api/learning/collections/' + str(fork['id'])
        fresh = request('GET', fork_base + '/summary', token=visitor)
        assert fresh['new'] == 3 and fresh['attemptsCount'] == 0, fresh
        request('GET', fork_base + '/summary', token=owner, expected=404)
        request('DELETE', '/api/collections/' + str(cid), token=owner)
        cleanup.remove((owner, cid))
        request('GET', base + '/summary', token=owner, expected=404)
        assert request('GET', fork_base + '/summary', token=visitor)['new'] == 3
        passed('Social allowlist/search, independent fork with zero evidence and deleted-source independence')
        print(json.dumps({'passed': len(checks), 'checks': checks}, ensure_ascii=False))
    finally:
        for token, cid in reversed(cleanup):
            request('DELETE', '/api/collections/' + str(cid), token=token)


if __name__ == '__main__':
    run()
