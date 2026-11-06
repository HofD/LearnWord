"""Read-only Compose config checks with synthetic secrets. Never prints config/keys."""
import json,os,subprocess
from pathlib import Path
repo=Path(__file__).resolve().parents[2]

def config(mode,overrides):
 env={k:v for k,v in os.environ.items() if not k.startswith('LW_AI_')}
 env.update({'LW_AI_OPENROUTER_API_KEY':'fixture-shared-key'});env.update(overrides)
 files=['-f','deploy/docker-compose.local.yml']
 if mode=='prod':files+=['-f','deploy/docker-compose.prod.yml']
 cmd=['docker','compose','--env-file',f'deploy/env/{mode}.env.example',*files,'config','--format','json']
 p=subprocess.run(cmd,cwd=repo,env=env,capture_output=True,text=True)
 if p.returncode:raise RuntimeError('Compose config validation failed; output suppressed to protect configuration.')
 return json.loads(p.stdout)['services']['learnword.webapi']['environment']

rows=[]
for mode in ('local','prod'):
 e=config(mode,{})
 assert e['AiCoach__Provider']==('Fake' if mode=='local' else 'OpenRouter')
 assert e['AiCoach__OpenRouter__Model']=='z-ai/glm-5.3-flash'
 assert e['AiCoach__ReasoningEffort']=='low' and str(e['AiCoach__MaxOutputTokens'])=='3000'
 assert e['AiCoach__OpenRouter__ApiKey']==e['AiCardGeneration__OpenRouter__ApiKey']=='fixture-shared-key'
 assert e['AiCardGeneration__OpenRouter__Model']=='google/gemma-4-26b-a4b-it:free'
 rows.append({'mode':mode,'coachProvider':e['AiCoach__Provider'],'coachModel':e['AiCoach__OpenRouter__Model'],'keyFallback':'passed','cardDefaultsPreserved':True})
e=config('prod',{'LW_AI_PROVIDER':'OpenRouter','LW_AI_OPENROUTER_MODEL':'google/gemma-4-26b-a4b-it','LW_AI_COACH_OPENROUTER_API_KEY':'fixture-coach-key'})
assert e['AiCoach__OpenRouter__ApiKey']=='fixture-coach-key' and e['AiCardGeneration__OpenRouter__ApiKey']=='fixture-shared-key'
assert e['AiCardGeneration__OpenRouter__Model']=='google/gemma-4-26b-a4b-it' and e['AiCoach__OpenRouter__Model']=='z-ai/glm-5.3-flash'
rows.append({'mode':'prod-separate-key','independentKeys':True,'cardGemmaPreserved':True,'coachGlm':True})
e=config('prod',{'LW_AI_COACH_PROVIDER':'Fake','LW_AI_COACH_OPENROUTER_MODEL':'coach-override','LW_AI_OPENROUTER_MODEL':'card-override'})
assert e['AiCoach__Provider']=='Fake' and e['AiCoach__OpenRouter__Model']=='coach-override' and e['AiCardGeneration__OpenRouter__Model']=='card-override'
rows.append({'mode':'prod-overrides','independentProviderAndModels':True})
Path(__file__).with_name('compose-results.json').write_text(json.dumps({'passed':len(rows),'checks':rows},indent=2)+'\n')
print(json.dumps({'passed':len(rows),'secretValuesPrinted':False}))
