"""Real local gateway acceptance. Uses disposable content and two confirmed local accounts.
Run from backend repository: python3 agent-runs/019-ai-coach/verify-coach.py
No credentials, tokens or private provider solutions are printed.
"""
import importlib.util
from concurrent.futures import ThreadPoolExecutor
import json
from pathlib import Path
import uuid

path = Path(__file__).resolve().parents[1] / '014-social-collections' / 'verify-social.py'
spec = importlib.util.spec_from_file_location('social_acceptance', path)
helper = importlib.util.module_from_spec(spec)
spec.loader.exec_module(helper)
request, account = helper.request, helper.account


def run():
    owner = account('coach-owner-local@example.com')
    visitor = account('coach-visitor-local@example.com')
    cleanup, checks = [], []

    def passed(name):
        checks.append(name)
        print('PASS:', name, flush=True)

    def create(label, values):
        c = request('POST', '/api/collections', {'name': 'Coach acceptance ' + label + ' ' + uuid.uuid4().hex[:8]}, owner, 201)
        cleanup.append((owner, c['id']))
        cards = [request('POST', '/api/cards', {'collectionId': c['id'], 'words': [
            {'value': value, 'translation': translation, 'transcription': ''}]}, owner, 201) for value, translation in values]
        return c, cards

    def review(card, outcome, sid=None):
        return request('POST', '/api/review/cards/' + str(card['id']) + '/review', {
            'outcome': outcome, 'requestId': str(uuid.uuid4()), 'responseTimeMs': 2100,
            **({'sessionId': sid} if sid else {})}, owner)

    def session(c, outcome):
        s = request('POST', '/api/learning/collections/' + str(c['id']) + '/sessions', {}, owner)
        for card in s['cards']:
            review(card, outcome, s['id'])
        return request('POST', '/api/learning/sessions/' + s['id'] + '/complete', {}, owner)

    def analysis(s):
        return '/api/learning/sessions/' + s['id'] + '/coach-analysis'

    def intervention(i):
        return '/api/learning/interventions/' + i['id']

    def snapshot(c, cards):
        details = request('GET', '/api/collections/' + str(c['id']), token=owner)
        srs = [{key: x.get(key) for key in ('id', 'dueDate', 'intervalDays', 'easeFactor', 'reviewCount', 'lastReviewedAt', 'learnt')} for x in details['cards']]
        evidence = {x['id']: request('GET', '/api/learning/cards/' + str(x['id']) + '/attempts', token=owner) for x in cards}
        states = {x['id']: request('GET', '/api/learning/cards/' + str(x['id']) + '/state', token=owner) for x in cards}
        return srs, evidence, states

    def no_solutions(value):
        if isinstance(value, dict):
            for k, v in value.items():
                assert k.lower() not in ('correctanswer', 'correctoptionid', 'expectedanswer', 'solution', 'useranswer'), k
                no_solutions(v)
        elif isinstance(value, list):
            for item in value:
                no_solutions(item)

    try:
        good, good_cards = create('successful', [('river', 'река'), ('lake', 'озеро')])
        done = session(good, 'Good')
        assert done['completedCardsCount'] == 2 and done['completedAt']
        endpoint = analysis(done)
        request('POST', endpoint, {'language': 'en'}, expected=401)
        request('GET', endpoint, expected=401)
        request('GET', endpoint, token=visitor, expected=404)
        request('POST', endpoint, {'language': 'en'}, visitor, 404)
        result = request('POST', endpoint, {'language': 'en'}, owner)
        assert result['status'] == 'Completed' and result['decision'] == 'None' and not result['interventions'], result
        assert request('POST', endpoint, {'language': 'ru'}, owner)['id'] == result['id']
        assert request('GET', endpoint, token=owner)['id'] == result['id']
        passed('A: successful adaptive session, None result, durable cache and anonymous/foreign session isolation')

        weak, weak_cards = create('weak', [('eventually', 'в конце концов'), ('resilient', 'стойкий')])
        for card in weak_cards:
            review(card, 'Again')
        done = session(weak, 'Again')
        endpoint = analysis(done)
        before = snapshot(weak, weak_cards)
        # Real PostgreSQL uniqueness/claim concurrency: only one durable analysis/intervention set.
        with ThreadPoolExecutor(max_workers=3) as pool:
            replies = list(pool.map(lambda _: request('POST', endpoint, {'language': 'ru'}, owner), range(3)))
        assert len({r['id'] for r in replies}) == 1
        result = request('GET', endpoint, token=owner)
        assert result['status'] == 'Completed' and result['decision'] == 'Intervention', result
        assert 1 <= len(result['interventions']) <= 3
        assert result['promptVersion'] and result['schemaVersion'] and result['provider'] and result['model']
        no_solutions(result)
        lesson = next(i for i in result['interventions'] if i['type'] in ('Explanation', 'MicroLesson'))
        practice = next(i for i in result['interventions'] if i['type'] == 'ExtraPractice')
        for i in (lesson, practice):
            p = intervention(i)
            for suffix in ('', '/accept', '/dismiss', '/complete'):
                method = 'GET' if not suffix else 'POST'
                request(method, p + suffix, {} if suffix else None, expected=401)
                request(method, p + suffix, {} if suffix else None, visitor, 404)
        lesson_path = intervention(lesson)
        accepted = request('POST', lesson_path + '/accept', {}, owner)
        assert accepted['status'] == 'Accepted'
        assert request('POST', lesson_path + '/accept', {}, owner)['startedAt'] == accepted['startedAt']
        completed = request('POST', lesson_path + '/complete', {}, owner)
        assert completed['status'] == 'Completed' and completed['completedAt']
        assert request('POST', lesson_path + '/complete', {}, owner)['completedAt'] == completed['completedAt']
        assert snapshot(weak, weak_cards) == before, 'lesson mutated deterministic learning state'
        passed('B: repeated evidence produces bounded grounded lesson; accept/complete idempotent, SRS/evidence/state unchanged')

        practice_path = intervention(practice)
        accepted = request('POST', practice_path + '/accept', {}, owner)
        assert accepted['status'] == 'Accepted' and 1 <= len(accepted['exercises']) <= 4
        no_solutions(accepted)
        request('POST', practice_path + '/exercises/' + str(uuid.uuid4()) + '/answer', {'optionId': 'a'}, owner, 404)
        translation = {card['id']: card['words'][0]['translation'] for card in weak_cards}[practice['cardId']]
        for index, ex in enumerate(accepted['exercises']):
            answer_path = practice_path + '/exercises/' + ex['id'] + '/answer'
            correct_option = next(o for o in ex['options'] if o['text'] == translation)
            wrong_option = next(o for o in ex['options'] if o['id'] != correct_option['id'])
            choice = correct_option if index % 2 == 0 else wrong_option
            body = {'optionId': choice['id'], 'responseTimeMs': 2400}
            request('POST', answer_path, body, expected=401)
            request('POST', answer_path, body, visitor, 404)
            answer = request('POST', answer_path, body, owner)
            assert answer['correct'] == (index % 2 == 0), answer
            retry = request('POST', answer_path, body, owner)
            assert retry['correct'] == answer['correct']
            other = wrong_option if index % 2 == 0 else correct_option
            request('POST', answer_path, {'optionId': other['id'], 'responseTimeMs': 2400}, owner, 409)
            no_solutions(answer)
        completed_practice = request('GET', practice_path, token=owner)
        assert completed_practice['status'] == 'Completed' and all(e['answered'] for e in completed_practice['exercises'])
        assert snapshot(weak, weak_cards) == before, 'practice mutated deterministic learning state'
        passed('C/E: server-side supplementary answers, correct/incorrect, duplicate/foreign protection, completion; no SRS/normal evidence/cache changes')

        # First future normal attempt is diagnostic only and revision aware.
        watched = next(c for c in weak_cards if c['id'] == lesson['cardId'])
        assert completed['evaluation']['status'] == 'NoFollowUpEvidence'
        review(watched, 'Good')
        evaluation = request('GET', lesson_path, token=owner)['evaluation']
        assert evaluation['status'] == 'FollowUpSuccess' and evaluation['postInterventionAttempts'] >= 1, evaluation
        assert evaluation['firstPostInterventionOutcome'] == 'Good' and evaluation['firstPostInterventionReviewedAt']
        passed('Follow-up diagnostic uses first future ordinary current-revision attempt only')

        request('PUT', '/api/collections/' + str(weak['id']) + '/visibility', {'visibility': 2}, owner)
        shared = '/api/social/collections/' + weak['publicId']
        details = request('GET', shared + '?search=eventually', token=visitor)
        assert details['totalCount'] == 1 and set(details['cards'][0]) == {'words'}
        assert all(set(w) == {'value', 'transcription', 'translation'} for w in details['cards'][0]['words'])
        def private_keys(value):
            if isinstance(value, dict):
                for key, child in value.items():
                    assert not any(term in key.lower() for term in ('coach', 'intervention', 'mastery', 'attempt', 'sessionid')), key
                    private_keys(child)
            elif isinstance(value, list):
                for child in value: private_keys(child)
        private_keys(details)
        fork = request('POST', shared + '/fork', token=visitor, expected=201)
        cleanup.append((visitor, fork['id']))
        fresh = request('GET', '/api/learning/collections/' + str(fork['id']) + '/summary', token=visitor)
        assert fresh['attemptsCount'] == 0 and fresh['new'] == len(weak_cards)
        passed('F: public/shared search content allowlist and fork with fresh learner state and no Coach leakage')

        # Fresh suggested work then edit, delete, and collection removal safety.
        stale_collection, stale_cards = create('stale', [('narrow', 'узкий'), ('broad', 'широкий')])
        for card in stale_cards:
            review(card, 'Again')
        stale_session = session(stale_collection, 'Again')
        stale_result = request('POST', analysis(stale_session), {'language': 'en'}, owner)
        for i in stale_result['interventions']:
            c = next(card for card in stale_cards if card['id'] == i['cardId'])
            if c.get('changed'):
                continue
            word = c['words'][0]
            request('PUT', '/api/cards/' + str(c['id']) + '/words/' + str(word['id']), {
                'value': word['value'], 'transcription': '', 'translation': word['translation'] + ' edited'}, owner)
            c['changed'] = True
        for i in stale_result['interventions']:
            assert request('GET', intervention(i), token=owner)['status'] == 'Stale'
            request('POST', intervention(i) + '/accept', {}, owner, 409)
        request('DELETE', '/api/cards/' + str(stale_cards[0]['id']), token=owner)
        for i in stale_result['interventions']:
            if i['cardId'] == stale_cards[0]['id']:
                assert request('GET', intervention(i), token=owner)['status'] == 'Unavailable'
        request('DELETE', '/api/collections/' + str(stale_collection['id']), token=owner)
        cleanup.remove((owner, stale_collection['id']))
        request('GET', analysis(stale_session), token=owner, expected=404)
        passed('D: edit invalidates old work, deleted content unavailable, deleted collection blocks Coach reads')
        report = {'passed': len(checks), 'checks': checks}
        Path(__file__).with_name('gateway-results.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
        print(json.dumps(report, ensure_ascii=False))
    finally:
        for token, cid in reversed(cleanup):
            request('DELETE', '/api/collections/' + str(cid), token=token)


if __name__ == '__main__':
    run()
