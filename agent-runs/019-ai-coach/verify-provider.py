"""Noncritical runtime configuration acceptance against local gateway.
Prepare disposable fixture, then run modes with Compose configuration described in final-acceptance.md.
Never prints credentials/tokens; writes only disposable resource IDs and evidence snapshots under /private/tmp.
"""
import importlib.util
import json
from pathlib import Path
import sys
import uuid
p=Path(__file__).resolve().parents[1]/'014-social-collections'/'verify-social.py'
s=importlib.util.spec_from_file_location('h',p);h=importlib.util.module_from_spec(s);s.loader.exec_module(h)
request=h.request
t=h.account('coach-provider-local@example.com')
f=Path('/private/tmp/learnword-coach-provider-fixture.json')
mode=sys.argv[1]
if mode=='prepare':
 c=request('POST','/api/collections',{'name':'Coach provider acceptance '+uuid.uuid4().hex[:6]},t,201)
 card=request('POST','/api/cards',{'collectionId':c['id'],'words':[{'value':'eventually','translation':'в конце концов','transcription':''}]},t,201)
 request('POST',f"/api/review/cards/{card['id']}/review",{'outcome':'Again'},t)
 se=request('POST',f"/api/learning/collections/{c['id']}/sessions",{},t)
 request('POST',f"/api/review/cards/{card['id']}/review",{'outcome':'Again','sessionId':se['id']},t)
 done=request('POST',f"/api/learning/sessions/{se['id']}/complete",{},t)
 f.write_text(json.dumps({'collectionId':c['id'],'cardId':card['id'],'sessionId':se['id'],'done':done}))
 print('PASS: provider fixture ordinary adaptive session completed')
else:
 fixture=json.loads(f.read_text()); sid=fixture['sessionId'];cid=fixture['collectionId'];cardid=fixture['cardId']
 base=f'/api/learning/sessions/{sid}'
 def state():
  return [request('GET',f'/api/collections/{cid}',token=t),request('GET',f'/api/learning/cards/{cardid}/attempts',token=t),request('GET',f'/api/learning/cards/{cardid}/state',token=t)]
 before=state()
 if mode=='cleanup':
  request('DELETE',f'/api/collections/{cid}',token=t)
  f.unlink();print('PASS: provider fixture soft-deleted')
 else:
  result=request('POST',base+'/coach-analysis',{'language':'en'},t)
  if mode in ('unavailable','missing-key'):
   expected='ai_provider_unavailable' if mode=='unavailable' else 'ai_provider_configuration_error'
   assert result['status']=='Failed' and result['failureCode']==expected and not result['interventions'],result
   if fixture.get('analysisId'):assert result['id']==fixture['analysisId']
   fixture['analysisId']=result['id'];f.write_text(json.dumps(fixture))
  elif mode=='disabled':
   assert result['status']=='Disabled' and not result['interventions'],result
   assert request('GET',base+'/coach-analysis',token=t)['status']=='Disabled'
  elif mode=='retry':
   assert result['status']=='Completed' and result['decision']=='Intervention',result
   assert result['id']==fixture['analysisId']
  else:raise ValueError(mode)
  assert request('GET',base,token=t)['completedAt']==fixture['done']['completedAt']
  assert state()==before,'provider operation mutated deterministic learner state'
  print('PASS:',mode,'noncritical, durable session unchanged, SRS/evidence/state unchanged')
