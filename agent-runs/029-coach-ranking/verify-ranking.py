"""Disposable local gateway acceptance; requires Fake Coach provider."""
import importlib.util, json, uuid
from pathlib import Path
p=Path(__file__).resolve().parents[1]/'014-social-collections'/'verify-social.py'
s=importlib.util.spec_from_file_location('helper',p);h=importlib.util.module_from_spec(s);s.loader.exec_module(h)
r=h.request;owner=h.account('coach-ranking-local@example.com')
c=r('POST','/api/collections',{'name':'Ranking acceptance '+uuid.uuid4().hex[:8]},owner,201)
try:
 cards=[r('POST','/api/cards',{'collectionId':c['id'],'words':[{'value':'fixture','translation':'пример','transcription':''}]},owner,201) for _ in range(10)]
 for card in cards[:5]: r('POST',f"/api/review/cards/{card['id']}/review",{'outcome':'Again'},owner)
 se=r('POST',f"/api/learning/collections/{c['id']}/sessions",{},owner)
 for i,card in enumerate(cards):
  outcome='Hard' if i<5 else 'Again'
  if i>=5: r('POST',f"/api/review/cards/{card['id']}/review",{'outcome':outcome},owner)
  r('POST',f"/api/review/cards/{card['id']}/review",{'outcome':outcome,'sessionId':se['id']},owner)
 base=f"/api/learning/sessions/{se['id']}"
 r('POST',base+'/complete',{},owner)
 def snapshot():
  return [[r('GET',f"/api/learning/cards/{card['id']}/attempts",token=owner),r('GET',f"/api/learning/cards/{card['id']}/state",token=owner)] for card in cards]+[r('GET',f"/api/collections/{c['id']}",token=owner)]
 before=snapshot()
 analysis=r('POST',base+'/coach-analysis',{'language':'en'},owner)
 d=r('GET',base+'/coach-diagnostics',token=owner)
 assert d['analysis']['provider']=='Fake',d['analysis']
 rows=d['context']['cards'];included=[x for x in rows if x['selectionReason']=='Included']
 assert {x['cardId'] for x in included}=={x['id'] for x in cards[5:]},rows
 assert sum(x['selectionReason']=='CandidateLimit' for x in rows)==5
 assert sorted(x['severityRank'] for x in included)==list(range(1,6))
 assert before==snapshot()
 Path(__file__).with_name('ranking-results.json').write_text(json.dumps(d['context'],indent=2))
 print('PASS: high IDs displace five lower-ID Hard cards; diagnostics match; learning data unchanged')
finally:
 r('DELETE',f"/api/collections/{c['id']}",token=owner,expected=200)
