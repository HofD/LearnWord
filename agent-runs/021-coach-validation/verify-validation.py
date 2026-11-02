"""Local gateway failure diagnostics acceptance; use ONLY with disposable provider-stub.js override.
No production/paid call. Companion stub returns a valid response with deliberately invalid exercise prompt.
"""
import importlib.util,json,uuid
from pathlib import Path
p=Path(__file__).resolve().parents[1]/'014-social-collections'/'verify-social.py';s=importlib.util.spec_from_file_location('h',p);h=importlib.util.module_from_spec(s);s.loader.exec_module(h)
r=h.request

def run():
 owner=h.account('coach-validation-owner@example.com');visitor=h.account('coach-validation-visitor@example.com');cid=None
 try:
  c=r('POST','/api/collections',{'name':'Coach validation fixture '+uuid.uuid4().hex[:6]},owner,201);cid=c['id']
  card=r('POST','/api/cards',{'collectionId':cid,'words':[{'value':'DO_NOT_EXPORT_WORD','translation':'DO_NOT_EXPORT_TRANSLATION','transcription':''}]},owner,201)
  r('POST',f"/api/review/cards/{card['id']}/review",{'outcome':'Again'},owner)
  se=r('POST',f'/api/learning/collections/{cid}/sessions',{},owner)
  r('POST',f"/api/review/cards/{card['id']}/review",{'outcome':'Again','sessionId':se['id']},owner)
  r('POST',f"/api/learning/sessions/{se['id']}/complete",{},owner)
  def state():return [r('GET',f'/api/collections/{cid}',token=owner),r('GET',f"/api/learning/cards/{card['id']}/state",token=owner),r('GET',f"/api/learning/cards/{card['id']}/attempts",token=owner)]
  before=state();base=f"/api/learning/sessions/{se['id']}";analysis=r('POST',base+'/coach-analysis',{'language':'en'},owner)
  assert analysis['status']=='Failed' and analysis['failureCode']=='ai_provider_invalid_response',analysis
  path=base+'/coach-diagnostics';r('GET',path,expected=401);r('GET',path,token=visitor,expected=404)
  d=r('GET',path,token=owner);a=d['analysis']
  assert a['validationCode']=='exercise_prompt_mismatch',d
  assert a['validationPath']=='$.interventions[0].exercises[0].prompt',d
  assert a['failureStage']=='OutputValidation',d
  assert d['mode']=='CapturedAtAnalysis' and d['providerCalled'] is True and d['outputCharacters']>0,d
  assert r('GET',path,token=owner)==d
  assert state()==before,'invalid Coach output/diagnostic read mutated normal learner state'
  text=json.dumps(d)
  for secret in ('DO_NOT_EXPORT_WORD','DO_NOT_EXPORT_TRANSLATION','local-validation-stub-only','What does','correctOptionId'):
   assert secret not in text,secret
  assert not analysis['interventions']
  result={'status':'passed','failureStage':a['failureStage'],'validationCode':a['validationCode'],'validationPath':a['validationPath'],'private':True,'normalStateUnchanged':True}
  Path(__file__).with_name('gateway-results.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
 finally:
  if cid is not None:r('DELETE',f'/api/collections/{cid}',token=owner)
if __name__=='__main__':run()
