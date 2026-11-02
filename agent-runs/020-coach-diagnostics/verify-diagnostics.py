"""Local two-user gateway diagnostics acceptance. Disposable fixtures only, no credentials printed."""
import importlib.util,json,uuid
from pathlib import Path
p=Path(__file__).resolve().parents[1]/'014-social-collections'/'verify-social.py';s=importlib.util.spec_from_file_location('h',p);h=importlib.util.module_from_spec(s);s.loader.exec_module(h)
r=h.request

def run():
 owner=h.account('coach-diag-owner@example.com');other=h.account('coach-diag-other@example.com');cleanup=[];checks=[]
 def passed(name):checks.append(name);print('PASS:',name,flush=True)
 def fixture(label,repeated,outcome):
  c=r('POST','/api/collections',{'name':'Diagnostics acceptance '+label+' '+uuid.uuid4().hex[:6]},owner,201);cleanup.append(c['id'])
  card=r('POST','/api/cards',{'collectionId':c['id'],'words':[{'value':'NEVER_EXPORT_CARD_TEXT','translation':'NEVER_EXPORT_TRANSLATION','transcription':''}]},owner,201)
  # Freeze a fresh-card plan first: a standalone Hard is neither Weak nor due immediately.
  se=r('POST',f"/api/learning/collections/{c['id']}/sessions",{},owner)
  if repeated:r('POST',f"/api/review/cards/{card['id']}/review",{'outcome':outcome},owner)
  r('POST',f"/api/review/cards/{card['id']}/review",{'outcome':outcome if repeated else 'Good','sessionId':se['id'],'responseTimeMs':1200},owner)
  r('POST',f"/api/learning/sessions/{se['id']}/complete",{},owner)
  return c,card,se
 def snapshot(c,card):
  return [r('GET',f"/api/collections/{c['id']}",token=owner),r('GET',f"/api/learning/cards/{card['id']}/attempts",token=owner),r('GET',f"/api/learning/cards/{card['id']}/state",token=owner)]
 def safe(d):
  txt=json.dumps(d)
  assert 'NEVER_EXPORT_CARD_TEXT' not in txt and 'NEVER_EXPORT_TRANSLATION' not in txt,txt
  def walk(o):
   if isinstance(o,dict):
    for k,v in o.items():
     assert k.lower() not in ('summary','email','userid','apikey','authorization','accesstoken','refreshtoken','correctoptionid','correctanswer','prompt','value','translation','currentcontent','rawresponse','payloadjson'),k
     walk(v)
   elif isinstance(o,list):
    for v in o:walk(v)
  walk(d)
 try:
  for repeated,outcome in ((False,'Good'),(True,'Again'),(True,'Hard')):
   c,card,se=fixture('repeat-'+outcome if repeated else 'single',repeated,outcome);base=f"/api/learning/sessions/{se['id']}";path=base+'/coach-diagnostics'
   r('GET',path,expected=401);r('GET',path,token=other,expected=404)
   before=snapshot(c,card);d=r('GET',path,token=owner)
   assert d['mode']=='ReconstructedNow' and not d.get('analysisId'),d
   safe(d);assert before==snapshot(c,card)
   analysis=r('POST',base+'/coach-analysis',{'language':'en'},owner)
   before=snapshot(c,card);d=r('GET',path,token=owner);again=r('GET',path,token=owner)
   assert d==again and d['mode']=='CapturedAtAnalysis' and d['analysisId']==analysis['id'],d
   assert d['context']['candidateCount']==(1 if repeated else 0),d
   assert d['analysis']['decisionSource']==('Provider' if repeated else 'NoCandidates'),d
   assert d['providerCalled']==repeated,d
   assert d['analysis']['decision']==('Intervention' if repeated and outcome=='Again' else 'None'),d
   assert before==snapshot(c,card);safe(d)
   passed((('Repeated '+outcome+': provider path') if repeated else 'Single: deterministic None')+' captured, text-free, owner-only and read-only')
   if repeated:
    r('POST',f"/api/review/cards/{card['id']}/review",{'outcome':'Good'},owner)
    word=card['words'][0]
    r('PUT',f"/api/cards/{card['id']}/words/{word['id']}",{'value':word['value'],'translation':'changed','transcription':''},owner)
    assert r('GET',path,token=owner)==d
    passed('Captured snapshot remains exact after later normal review and content revision')
   r('DELETE',f"/api/collections/{c['id']}",token=owner);cleanup.remove(c['id']);r('GET',path,token=owner,expected=404)
  Path(__file__).with_name('gateway-results.json').write_text(json.dumps({'passed':len(checks),'checks':checks},indent=2)+'\n')
  print('All diagnostics gateway checks passed')
 finally:
  for cid in cleanup:r('DELETE',f'/api/collections/{cid}',token=owner)

if __name__=='__main__':run()
