"""Local-only acceptance through the real gateway. Never prints credentials/tokens."""
import json
import re
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid

BASE = 'http://localhost:5100'
checks = []


def request(method, path, body=None, token=None, expected=200):
    headers = {'Content-Type': 'application/json'}
    if token:
        headers['Authorization'] = 'Bearer ' + token
    data = None if body is None else json.dumps(body).encode()
    req = urllib.request.Request(BASE + path, data, headers, method=method)
    try:
        response = urllib.request.urlopen(req, timeout=30)
    except urllib.error.HTTPError as error:
        response = error
    payload = response.read().decode()
    assert response.status == expected, (method, path, expected, response.status, payload[:500])
    return json.loads(payload) if payload else None


def account(email):
    password = 'Agent-test1!'
    try:
        return request('POST', '/api/auth/login', {'email': email, 'password': password})['token']
    except AssertionError:
        pass
    request('POST', '/api/account/register', {'email': email, 'password': password})
    for _ in range(20):
        messages = json.load(urllib.request.urlopen('http://localhost:8025/api/v1/messages'))
        for message in messages['messages']:
            if not any(recipient.get('Address') == email for recipient in message.get('To', [])):
                continue
            detail = json.load(urllib.request.urlopen('http://localhost:8025/api/v1/message/' + message['ID']))
            text = detail.get('Text', '') + detail.get('HTML', '')
            match = re.search(r'(?:https?://[^\s"<>]+)?[^\s"<>]*[?&]userId=([^&\s"<>]+)&(?:amp;)?code=([^\s"<>]+)', text)
            if match:
                request('GET', '/api/account/confirm?userId=' + match[1] + '&code=' + match[2])
                return request('POST', '/api/auth/login', {'email': email, 'password': password})['token']
        time.sleep(.5)
    raise AssertionError('No confirmation message for local account')


def check(name):
    checks.append(name)
    print('PASS:', name, flush=True)


if __name__ == '__main__':
    suffix = uuid.uuid4().hex[:10]
    owner = account('social-owner-' + suffix + '@example.com')
    visitor = account('social-visitor-' + suffix + '@example.com')
    name = 'Social acceptance ' + suffix
    collection = request('POST', '/api/collections', {'name': name}, owner, 201)
    cid, public_id = collection['id'], collection['publicId']
    shared = '/api/social/collections/' + public_id
    assert collection['visibility'] == 0
    request('GET', shared, expected=401)
    request('GET', '/api/social/collections', expected=401)
    request('GET', shared, token=visitor, expected=404)
    request('GET', '/api/collections/' + str(cid), token=visitor, expected=404)
    check('Private default and anonymous/foreign access boundaries')
    card = request('POST', '/api/cards', {'collectionId': cid, 'words': [
        {'value': 'river', 'transcription': 'rɪvə', 'translation': 'река'},
        {'value': 'stream', 'transcription': 'striːm', 'translation': 'поток'}]}, owner, 201)
    request('POST', '/api/review/cards/' + str(card['id']) + '/review', {'outcome': 'Easy'}, owner)
    request('PUT', '/api/collections/' + str(cid) + '/visibility', {'visibility': 99}, owner, 400)
    for method, path, body in [
        ('PUT', '/api/collections/' + str(cid) + '/visibility', {'visibility': 2}),
        ('PUT', '/api/collections/' + str(cid), {'name': 'hijack'}),
        ('DELETE', '/api/collections/' + str(cid), None),
        ('POST', '/api/cards', {'collectionId': cid, 'words': []}),
    ]:
        # Existing card write convention uses 403, collection ownership uses 404.
        request(method, path, body, visitor, 403 if path == '/api/cards' else 404)
    check('Owner-only visibility, rename, deletion and foreign-parent card creation')
    request('PUT', '/api/collections/' + str(cid) + '/visibility', {'visibility': 1}, owner)
    details = request('GET', shared + '?page=1&pageSize=1', token=visitor)
    assert details['cardsCount'] == 1 and len(details['cards']) == 1
    assert not details['isOwner'] and details.get('ownerCollectionId') is None
    assert set(details['cards'][0]) == {'words'}
    assert all(set(word) == {'value', 'transcription', 'translation'} for word in details['cards'][0]['words'])
    assert '@' not in details['author']
    assert request('GET', '/api/social/collections?search=' + urllib.parse.quote(name), token=visitor)['totalCount'] == 0
    check('Unlisted shared content, safe DTO and exclusion from Explore')
    request('PUT', '/api/collections/' + str(cid) + '/visibility', {'visibility': 2}, owner)
    catalog = request('GET', '/api/social/collections?page=1&pageSize=1&search=' + urllib.parse.quote(suffix), token=visitor)
    assert catalog['totalCount'] == 1 and catalog['items'][0]['publicId'] == public_id
    assert request('GET', '/api/social/collections?page=2&pageSize=1&search=' + suffix, token=visitor)['items'] == []
    check('Public Explore, search and database pagination')
    request('POST', shared + '/fork', token=owner, expected=409)
    fork = request('POST', shared + '/fork', token=visitor, expected=201)
    fid = fork['id']
    assert fork['visibility'] == 0 and fork['forkedFromCollectionId'] == cid
    assert fork['cards'][0]['id'] != card['id']
    assert fork['cards'][0]['reviewCount'] == 0 and not fork['cards'][0]['learnt']
    assert [w['value'] for w in fork['cards'][0]['words']] == ['river', 'stream']
    request('GET', '/api/collections/' + str(fid) + '/review', token=visitor)
    check('Deep private fork, fresh SRS, new ownership and own-source conflict')
    request('PUT', '/api/collections/' + str(cid), {'name': name + ' renamed'}, owner)
    assert request('GET', shared, token=visitor)['name'].endswith(' renamed')
    request('PUT', '/api/collections/' + str(cid) + '/visibility', {'visibility': 0}, owner)
    request('GET', shared, token=visitor, expected=404)
    request('POST', shared + '/fork', token=visitor, expected=404)
    assert request('GET', shared, token=owner)['isOwner']
    request('DELETE', '/api/collections/' + str(cid), token=owner)
    copy = request('GET', '/api/collections/' + str(fid), token=visitor)
    assert copy['cards'][0]['words'][0]['value'] == 'river'
    assert copy['forkedFromCollectionId'] == cid
    check('Stable renamed URL, immediate revocation, owner access and source-deletion independence')
    request('DELETE', '/api/collections/' + str(fid), token=visitor)
    print(json.dumps({'passed': len(checks), 'checks': checks}, ensure_ascii=False))
