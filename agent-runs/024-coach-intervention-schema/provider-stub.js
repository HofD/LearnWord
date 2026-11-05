// Synthetic local acceptance only. Ajv independently checks the exact schema sent by the API.
const http = require('http');
const Ajv = require('/app/node_modules/ajv');
const ajv = new Ajv({strict:false,allErrors:true});
const modes=new Set(['lesson','explanation','practice','mixed','practiceLesson','duplicate','nullLesson','emptyPractice','unknownType','none']);
http.createServer((req,res)=>{
 let raw='';req.on('data',chunk=>{raw+=chunk;if(raw.length>100000)req.destroy();});
 req.on('end',()=>{try {
  const body=JSON.parse(raw);
  if(body.provider?.require_parameters!==true || body.max_tokens!==3000 || 'reasoning' in body)throw Error();
  const system=body.messages.find(m=>m.role==='system').content;
  const user=body.messages.find(m=>m.role==='user').content;
  const context=JSON.parse(user.slice(user.indexOf('\n')+1)),card=context.cards[0],word=card.currentContent[0];
  const mode=word.value.split('_').pop();if(!modes.has(mode))throw Error();
  const schema=body.response_format?.json_schema?.schema;
  if(body.response_format?.json_schema?.strict!==true || !ajv.validateSchema(schema))throw Error();
  const validate=ajv.compile(schema);
  const observation={cardId:card.cardId,learningRevision:card.learningRevision,type:'RepeatedFailure',evidenceAttemptIds:card.recentAttempts.filter(a=>a.outcome==='Again').slice(0,2).map(a=>a.id),evidenceSummary:'Two reviews marked Again.',explanation:'Review the supplied pair.'};
  const lesson={explanation:'Review this supplied meaning.',examples:[],tips:['Try recalling the pair.']};
  const exercise={type:'RecognitionChoice',prompt:word.value,options:[{id:'a',text:word.translation},{id:'b',text:'Another meaning'}],correctOptionId:'a',explanation:'Recall the supplied pair.'};
  const intervention={cardId:card.cardId,learningRevision:card.learningRevision,type:'MicroLesson',title:'Short recap',lesson,exercises:[]};
  if(mode==='explanation')intervention.type='Explanation';
  if(['practice','practiceLesson','emptyPractice'].includes(mode)){intervention.type='ExtraPractice';intervention.lesson=null;intervention.exercises=[exercise];}
  if(mode==='mixed')intervention.exercises=[exercise];
  if(mode==='practiceLesson')intervention.lesson=lesson;
  if(mode==='nullLesson')intervention.lesson=null;
  if(mode==='emptyPractice')intervention.exercises=[];
  if(mode==='unknownType')intervention.type='PRIVATE_UNKNOWN_TYPE';
  const result={decision:'Intervention',summary:'A short recap may help.',observations:[observation],interventions:[intervention]};
  if(mode==='duplicate')result.observations.push({...observation,type:'LowMastery'});
  if(mode==='none'){result.decision='None';result.observations=[];result.interventions=[];}
  const expectedSchemaValid=['lesson','explanation','practice','duplicate','none'].includes(mode);
  if(validate(result)!==expectedSchemaValid)throw Error();
  res.writeHead(200,{'Content-Type':'application/json'});
  res.end(JSON.stringify({choices:[{message:{content:JSON.stringify(result)},finish_reason:'stop'}],usage:{prompt_tokens:123,completion_tokens:45,total_tokens:168}}));
  process.stdout.write('Independent schema check passed: '+mode+'; valid='+expectedSchemaValid+'\n');
 }catch{res.writeHead(500);res.end('{}');}});
}).listen(8080,'0.0.0.0');
