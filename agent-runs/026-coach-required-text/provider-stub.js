// Local independent validation of the exact outgoing Coach schema; synthetic content only.
const http=require('http');
const Ajv=require('/app/node_modules/ajv');
const ajv=new Ajv({strict:false,allErrors:true});
const cases=new Set(['validLesson','emptyObservation','blankObservation','emptyEvidence','emptyTitle','emptyLesson','emptyExample','emptyTip','emptyOption','optionalExercise','noneEmptySummary','interventionEmptySummary']);
http.createServer((req,res)=>{let raw='';req.on('data',c=>{raw+=c;if(raw.length>100000)req.destroy();});req.on('end',()=>{try{
 const body=JSON.parse(raw);if(body.provider?.require_parameters!==true||body.max_tokens!==3000||'reasoning' in body)throw Error();
 const user=body.messages.find(m=>m.role==='user').content;
 const context=JSON.parse(user.slice(user.indexOf('\n')+1)),card=context.cards[0],word=card.currentContent[0],mode=word.value.split('_').pop();
 if(!cases.has(mode))throw Error();
 const schema=body.response_format?.json_schema?.schema;
 if(body.response_format?.json_schema?.strict!==true||!ajv.validateSchema(schema))throw Error();
 const validate=ajv.compile(schema);
 const observation={cardId:card.cardId,learningRevision:card.learningRevision,type:'RepeatedFailure',evidenceAttemptIds:card.recentAttempts.filter(a=>a.outcome==='Again').slice(0,2).map(a=>a.id),evidenceSummary:'Two reviews marked Again.',explanation:'Review the supplied meaning.'};
 const lesson={explanation:'Recall this supplied meaning.',examples:[],tips:[]};
 const exercise={type:'RecognitionChoice',prompt:word.value,options:[{id:'a',text:word.translation},{id:'b',text:'Another meaning'}],correctOptionId:'a',explanation:''};
 const intervention={cardId:card.cardId,learningRevision:card.learningRevision,type:'MicroLesson',title:'Short recap',lesson,exercises:[]};
 const result={decision:'Intervention',summary:'A short recap may help.',observations:[observation],interventions:[intervention]};
 if(mode==='emptyObservation')observation.explanation='';
 if(mode==='blankObservation')observation.explanation=' \t\n';
 if(mode==='emptyEvidence')observation.evidenceSummary='';
 if(mode==='emptyTitle')intervention.title='';
 if(mode==='emptyLesson')lesson.explanation='';
 if(mode==='emptyExample')lesson.examples=[{source:'',target:'Some meaning'}];
 if(mode==='emptyTip')lesson.tips=[''];
 if(['emptyOption','optionalExercise'].includes(mode)){intervention.type='ExtraPractice';intervention.lesson=null;intervention.exercises=[exercise];}
 if(mode==='emptyOption')exercise.options[0].text='';
 if(mode==='noneEmptySummary'){result.decision='None';result.summary='';result.observations=[];result.interventions=[];}
 if(mode==='interventionEmptySummary')result.summary='';
 const expected=['validLesson','blankObservation','optionalExercise','noneEmptySummary','interventionEmptySummary'].includes(mode);
 if(validate(result)!==expected)throw Error();
 res.writeHead(200,{'Content-Type':'application/json'});res.end(JSON.stringify({choices:[{message:{content:JSON.stringify(result)},finish_reason:'stop'}],usage:{prompt_tokens:123,completion_tokens:45,total_tokens:168}}));
 process.stdout.write('Independent text check passed: '+mode+'; valid='+expected+'\n');
 }catch{res.writeHead(500);res.end('{}');}});}).listen(8080,'0.0.0.0');
