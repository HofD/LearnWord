"""Local Docker gateway acceptance with companion Ajv HTTP stub; no paid/production requests."""
import importlib.util,json,uuid
from pathlib import Path
p=Path(__file__).resolve().parents[1]/'014-social-collections'/'verify-social.py';s=importlib.util.spec_from_file_location('h',p);h=importlib.util.module_from_spec(s);s.loader.exec_module(h);r=h.request

def run():
 owner=h.account('coach-schema-owner@example.com');visitor=h.account('coach-schema-visitor@example.com');results=[]
 cases={'lesson':None,'explanation':None,'practice':None,'mixed':'lesson_has_exercises','practiceLesson':'practice_has_lesson','duplicate':'observation_duplicate_card','nullLesson':'lesson_required','emptyPractice':'practice_requires_exercises','unknownType':'enum_invalid','none':None}
 for mode,expected in cases.items():
  cid=None
  try:
   collection=r('POST','/api/collections',{'name':'Schema fixture '+uuid.uuid4().hex[:6]},owner,201);cid=collection['id']
   card=r('POST','/api/cards',{'collectionId':cid,'words':[{'value':'PRIVATE_WORD_'+mode,'translation':'PRIVATE_TRANSLATION','transcription':''}]},owner,201)
   r('POST',f"/api/review/cards/{card['id']}/review",{'outcome':'Again'},owner)
   session=r('POST',f'/api/learning/collections/{cid}/sessions',{},owner)
   r('POST',f"/api/review/cards/{card['id']}/review",{'outcome':'Again','sessionId':session['id']},owner)
   r('POST',f"/api/learning/sessions/{session['id']}/complete",{},owner)
   def state():return [r('GET',f'/api/collections/{cid}',token=owner),r('GET',f"/api/learning/cards/{card['id']}/state",token=owner),r('GET',f"/api/learning/cards/{card['id']}/attempts",token=owner)]
   before=state();base=f"/api/learning/sessions/{session['id']}";a=r('POST',base+'/coach-analysis',{'language':'en'},owner)
   path=base+'/coach-diagnostics';r('GET',path,expected=401);r('GET',path,token=visitor,expected=404);d=r('GET',path,token=owner)
   assert a['promptVersion']=='coach-v4' and d['analysis']['promptVersion']=='coach-v4',d
   assert d['providerResponse']['httpStatus']==200 and d['providerResponse']['finishReason']=='stop',d
   if expected:
    assert a['status']=='Failed' and a['failureCode']=='ai_provider_invalid_response',a
    assert d['analysis']['validationCode']==expected and d['analysis']['failureStage']=='OutputValidation',d
    assert not a['interventions']
   else:
    assert a['status']=='Completed' and a['failureCode'] is None,a
    assert len(a['interventions'])==(0 if mode=='none' else 1),a
   assert state()==before and r('GET',path,token=owner)==d
   assert 'PRIVATE_' not in json.dumps(d) and 'local-validation-stub-only' not in json.dumps(d)
   results.append({'case':mode,'status':a['status'],'validationCode':d['analysis']['validationCode'],'validationPath':d['analysis']['validationPath'],'private':True,'normalStateUnchanged':True})
  finally:
   if cid is not None:r('DELETE',f'/api/collections/{cid}',token=owner)
 Path(__file__).with_name('gateway-results.json').write_text(json.dumps(results,indent=2)+'\n');print(json.dumps({'passed':len(results),'cases':[x['case'] for x in results]}))
if __name__=='__main__':run()
