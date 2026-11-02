// Disposable local HTTP stub: never logs request text, credentials or model output.
const http = require('http');
http.createServer((req,res) => {
 let text='';req.on('data',chunk=>{text+=chunk;if(text.length>100000)req.destroy();});
 req.on('end',()=>{
  try {
   const body=JSON.parse(text);
   if(body.provider?.require_parameters!==true){res.writeHead(400,{'Content-Type':'application/json'});res.end(JSON.stringify({error:{message:'require_parameters missing'}}));return;}
   const user=body.messages.find(m=>m.role==='user').content;
   const context=JSON.parse(user.slice(user.indexOf('\n')+1));
   const card=context.cards[0],word=card.currentContent[0];
   const result={decision:'Intervention',summary:'A short recap may help.',observations:[{cardId:card.cardId,learningRevision:card.learningRevision,type:'RepeatedFailure',evidenceAttemptIds:card.recentAttempts.filter(a=>a.outcome==='Again').slice(0,2).map(a=>a.id),evidenceSummary:'Two reviews marked Again.',explanation:'Review the supplied meaning.'}],interventions:[{cardId:card.cardId,learningRevision:card.learningRevision,type:'ExtraPractice',title:'Short practice',lesson:null,exercises:[{type:'RecognitionChoice',prompt:'What does '+word.value+' mean?',options:[{id:'a',text:word.translation},{id:'b',text:'Different meaning'}],correctOptionId:'a',explanation:'Review this pair.'}]}]};
   res.writeHead(200,{'Content-Type':'application/json'});res.end(JSON.stringify({choices:[{message:{content:JSON.stringify(result)},finish_reason:'stop'}]}));
   process.stdout.write('Structured-output routing required; test reply sent.\n');
  }catch{res.writeHead(500);res.end('{}');}
 });
}).listen(8080,'0.0.0.0');
