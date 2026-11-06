"""Run only with local release HTTP stub overrides; no paid/production requests."""
import importlib.util,json,sys,uuid
from pathlib import Path
p=Path(__file__).resolve().parents[1]/'014-social-collections'/'verify-social.py';s=importlib.util.spec_from_file_location('h',p);h=importlib.util.module_from_spec(s);s.loader.exec_module(h);r=h.request
mode=sys.argv[1];assert mode in ('success','invalid','missing-key','disabled','fake')
owner=h.account('coach-glm-release-owner@example.com');visitor=h.account('coach-glm-release-visitor@example.com');cid=None
try:
 collection=r('POST','/api/collections',{'name':'GLM release '+mode+' '+uuid.uuid4().hex[:6]},owner,201);cid=collection['id']
 collection_before=r('GET',f'/api/collections/{cid}',token=owner)
 draftpath=f'/api/collections/{cid}/ai/generate-cards'
 r('POST',draftpath,{'sourceText':'Synthetic draft source','maxCards':1},expected=401)
 r('POST',draftpath,{'sourceText':'Synthetic draft source','maxCards':1},token=visitor,expected=403)
 drafts=r('POST',draftpath,{'sourceText':'Synthetic draft source','sourceLanguage':'en','targetLanguage':'ru','maxCards':1},owner)
 assert len(drafts['cards'])==1 and drafts['cards'][0]['value']=='fixture-draft',drafts
 assert r('GET',f'/api/collections/{cid}',token=owner)==collection_before,'draft preview persisted cards'
 card=r('POST','/api/cards',{'collectionId':cid,'words':[{'value':'PRIVATE_WORD_'+mode,'translation':'PRIVATE_TRANSLATION','transcription':''}]},owner,201)
 r('POST',f"/api/review/cards/{card['id']}/review",{'outcome':'Again'},owner)
 session=r('POST',f'/api/learning/collections/{cid}/sessions',{},owner)
 r('POST',f"/api/review/cards/{card['id']}/review",{'outcome':'Again','sessionId':session['id']},owner)
 r('POST',f"/api/learning/sessions/{session['id']}/complete",{},owner)
 def state():return [r('GET',f'/api/collections/{cid}',token=owner),r('GET',f"/api/learning/cards/{card['id']}/state",token=owner),r('GET',f"/api/learning/cards/{card['id']}/attempts",token=owner)]
 before=state();base=f"/api/learning/sessions/{session['id']}";analysispath=base+'/coach-analysis'
 r('POST',analysispath,{'language':'en'},expected=401);r('POST',analysispath,{'language':'en'},token=visitor,expected=404)
 a=r('POST',analysispath,{'language':'en'},owner)
 assert a['promptVersion']=='coach-v6',a
 if mode in ('success','fake'):
  assert a['status']=='Completed' and a['decision']=='Intervention' and a['interventions'],a
  if mode=='success':assert a['model']=='z-ai/glm-5.3-flash' and a['provider']=='OpenRouter',a
  else:assert a['provider']=='Fake' and a['model']=='deterministic-coach-v1',a
  intervention=next(i for i in a['interventions'] if i['type']!='ExtraPractice')
  r('POST',f"/api/learning/interventions/{intervention['id']}/accept",{},owner)
  r('POST',f"/api/learning/interventions/{intervention['id']}/complete",{},owner)
 elif mode=='disabled':assert a['status']=='Disabled' and not a['interventions'],a
 else:
  expected='ai_provider_invalid_response' if mode=='invalid' else 'ai_provider_configuration_error'
  assert a['status']=='Failed' and a['failureCode']==expected and not a['interventions'],a
 assert state()==before,'Coach/draft operation mutated ordinary learning state'
 path=base+'/coach-diagnostics';r('GET',path,expected=401);r('GET',path,token=visitor,expected=404);d=r('GET',path,token=owner)
 if mode=='invalid':assert d['analysis']['validationCode']=='intervention_requires_observations',d
 if mode=='missing-key':assert d['analysis']['failureStage']=='Configuration' and d['providerResponse'] is None,d
 if mode=='success':assert d['providerResponse']['httpStatus']==200 and d['providerResponse']['reasoningTokens']==40,d
 text=json.dumps(d)
 for secret in ('PRIVATE_','fixture-card-key','fixture-coach-key','Recall the supplied pair.'):
  assert secret not in text,secret
 repeated=r('GET',path,token=owner)
 if mode=='disabled':
  assert repeated['mode']=='ReconstructedNow' and repeated['analysis']['status']=='Disabled' and repeated['analysisId'] is None,repeated
 else:assert repeated==d
 result={'mode':mode,'cardGeneration':'Gemma wire path passed','coachStatus':a['status'],'coachModel':a.get('model'),'promptVersion':a['promptVersion'],'failureCode':a.get('failureCode'),'private':True,'normalStateUnchanged':True}
 out=Path(__file__).with_name('gateway-results.json');rows=json.loads(out.read_text()) if out.exists() else [];rows=[x for x in rows if x['mode']!=mode];rows.append(result);out.write_text(json.dumps(rows,indent=2)+'\n');print(json.dumps(result))
finally:
 if cid is not None:r('DELETE',f'/api/collections/{cid}',token=owner)
