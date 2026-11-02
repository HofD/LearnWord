"""Run only against local Docker with companion HTTP stub override; no paid request."""
import importlib.util,json,uuid
from pathlib import Path
p=Path(__file__).resolve().parents[1]/'014-social-collections'/'verify-social.py'
s=importlib.util.spec_from_file_location('h',p);h=importlib.util.module_from_spec(s);s.loader.exec_module(h);r=h.request

def run():
 owner=h.account('coach-response-owner@example.com');visitor=h.account('coach-response-visitor@example.com');results=[]
 for mode in ('fenced','length','prefix','syntax','valid'):
  cid=None
  try:
   c=r('POST','/api/collections',{'name':'Response diagnostic '+uuid.uuid4().hex[:6]},owner,201);cid=c['id']
   card=r('POST','/api/cards',{'collectionId':cid,'words':[{'value':'PRIVATE_WORD_'+mode,'translation':'PRIVATE_TRANSLATION','transcription':''}]},owner,201)
   r('POST',f"/api/review/cards/{card['id']}/review",{'outcome':'Again'},owner)
   se=r('POST',f'/api/learning/collections/{cid}/sessions',{},owner)
   r('POST',f"/api/review/cards/{card['id']}/review",{'outcome':'Again','sessionId':se['id']},owner)
   r('POST',f"/api/learning/sessions/{se['id']}/complete",{},owner)
   def state():return [r('GET',f'/api/collections/{cid}',token=owner),r('GET',f"/api/learning/cards/{card['id']}/state",token=owner),r('GET',f"/api/learning/cards/{card['id']}/attempts",token=owner)]
   before=state();base=f"/api/learning/sessions/{se['id']}";a=r('POST',base+'/coach-analysis',{'language':'en'},owner)
   path=base+'/coach-diagnostics';r('GET',path,expected=401);r('GET',path,token=visitor,expected=404)
   d=r('GET',path,token=owner);detail=d['analysis'];pr=d['providerResponse'];fmt=d['outputFormat']
   assert pr['httpStatus']==200 and pr['finishReason']==('length' if mode=='length' else 'stop'),d
   assert pr['promptTokens']==123 and pr['completionTokens']==(3000 if mode=='length' else 45),d
   assert pr['totalTokens']==123+pr['completionTokens'] and pr['reasoningTokens']==(2800 if mode=='length' else 0),d
   assert fmt['startsWithCodeFence']==(mode=='fenced') and fmt['endsWithCodeFence']==(mode=='fenced'),d
   assert fmt['startsWithJsonObject']==(mode in ('length','syntax','valid')),d
   if mode=='valid':
    assert a['status']=='Completed' and detail['validationCode'] is None and detail['jsonLineNumber'] is None,d
   else:
    assert a['status']=='Failed' and detail['failureStage']=='OutputValidation' and detail['validationCode']=='json_malformed',d
    assert isinstance(detail['jsonLineNumber'],int) and isinstance(detail['jsonBytePositionInLine'],int),d
    if mode=='syntax':assert detail['jsonLineNumber']>=1,d
   assert state()==before and r('GET',path,token=owner)==d
   assert not a['interventions']
   text=json.dumps(d)
   for secret in ('PRIVATE_','local-validation-stub-only','```','Bearer'):
    assert secret not in text,secret
   results.append({'case':mode,'status':a['status'],'providerResponse':pr,'outputFormat':fmt,'jsonLineNumber':detail['jsonLineNumber'],'jsonBytePositionInLine':detail['jsonBytePositionInLine'],'private':True,'normalStateUnchanged':True})
  finally:
   if cid is not None:r('DELETE',f'/api/collections/{cid}',token=owner)
 Path(__file__).with_name('gateway-results.json').write_text(json.dumps(results,indent=2)+'\n');print(json.dumps({'passed':len(results),'cases':[x['case'] for x in results]}))
if __name__=='__main__':run()
