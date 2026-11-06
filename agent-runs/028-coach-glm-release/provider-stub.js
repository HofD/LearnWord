// Local wire-isolation acceptance. No request text, headers or credentials are logged.
const http=require('http');
http.createServer((req,res)=>{let raw='';req.on('data',c=>{raw+=c;if(raw.length>100000)req.destroy();});req.on('end',()=>{try{
 const body=JSON.parse(raw);let result;
 if(req.url==='/cards'){
  if(body.model!=='google/gemma-4-26b-a4b-it'||req.headers.authorization!=='Bearer fixture-card-key'||'reasoning' in body||'provider' in body||'max_tokens' in body)throw Error();
  result={cards:[{value:'fixture-draft',translation:'fixture meaning',transcription:'',example:'',explanation:'',difficulty:'A1'}]};
  process.stdout.write(JSON.stringify({kind:'cards',model:body.model,coachParametersAbsent:true})+'\n');
 }else if(req.url==='/coach'){
  if(body.model!=='z-ai/glm-5.3-flash'||req.headers.authorization!=='Bearer fixture-coach-key'||body.reasoning?.effort!=='low'||body.max_tokens!==3000||body.provider?.require_parameters!==true)throw Error();
  const user=body.messages.find(m=>m.role==='user').content;
  const context=JSON.parse(user.slice(user.indexOf('\n')+1)),card=context.cards[0],word=card.currentContent[0];
  const invalid=word.value.endsWith('_invalid');
  result={decision:'Intervention',summary:'A short recap may help.',observations:invalid?[]:[{cardId:card.cardId,learningRevision:card.learningRevision,type:'RepeatedFailure',evidenceAttemptIds:card.recentAttempts.filter(a=>a.outcome==='Again').slice(0,2).map(a=>a.id),evidenceSummary:'Two reviews marked Again.',explanation:'Review the supplied meaning.'}],interventions:invalid?[]:[{cardId:card.cardId,learningRevision:card.learningRevision,type:'MicroLesson',title:'Short recap',lesson:{explanation:'Recall the supplied pair.',examples:[],tips:[]},exercises:[]}]};
  process.stdout.write(JSON.stringify({kind:'coach',model:body.model,reasoningEffort:body.reasoning.effort,maxOutputTokens:body.max_tokens})+'\n');
 }else throw Error();
 res.writeHead(200,{'Content-Type':'application/json'});res.end(JSON.stringify({choices:[{message:{content:JSON.stringify(result)},finish_reason:'stop'}],usage:{prompt_tokens:123,completion_tokens:80,total_tokens:203,completion_tokens_details:{reasoning_tokens:40}}}));
 }catch{res.writeHead(500);res.end('{}');}});}).listen(8080,'0.0.0.0');
