// Local synthetic response variants; never log prompts, credentials or generated content.
const http = require('http');
http.createServer((req,res) => {
 let raw=''; req.on('data',chunk=>{raw+=chunk;if(raw.length>100000)req.destroy();});
 req.on('end',()=>{try {
  const body=JSON.parse(raw);
  if(body.provider?.require_parameters!==true)throw Error();
  const user=body.messages.find(m=>m.role==='user').content;
  const context=JSON.parse(user.slice(user.indexOf('\n')+1));
  const word=context.cards[0].currentContent[0];
  const mode=word.value.split('_').pop();
  const valid=JSON.stringify({decision:'None',summary:word.value+' '+word.translation,observations:[],interventions:[]});
  let content=valid;
  if(mode==='fenced')content='```json\n'+valid+'\n```';
  if(mode==='length')content=valid.slice(0,-2);
  if(mode==='prefix')content='PRIVATE_PREFIX\n'+valid;
  if(mode==='syntax')content='{\n"decision":"None",\n"summary":"PRIVATE_MARKER",\n"observations":[],\n"interventions":[}\n';
  const tokens=mode==='length'?3000:45;
  res.writeHead(200,{'Content-Type':'application/json'});
  res.end(JSON.stringify({choices:[{message:{content},finish_reason:mode==='length'?'length':'stop'}],usage:{prompt_tokens:123,completion_tokens:tokens,total_tokens:123+tokens,completion_tokens_details:{reasoning_tokens:mode==='length'?2800:0}}}));
  process.stdout.write('Synthetic response delivered.\n');
 }catch{res.writeHead(500);res.end('{}');}});
}).listen(8080,'0.0.0.0');
