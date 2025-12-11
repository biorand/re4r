import json5 as json
import json as std_json
import os

# Path to areas.json
areas_json_path = os.path.join('src', 'IntelOrca.Biohazard.BioRand.RE4R', 'data', 'areas.json')
areas2_json_path = os.path.join('src', 'IntelOrca.Biohazard.BioRand.RE4R', 'data', 'areas2.json')

# Read areas.json
with open(areas_json_path, 'r') as f:
    data = json.load(f)

for area in data['areas']:
    if 'description' in area:
        del area['description']
    if 'restrictions' in area:
        del area['restrictions']
    if 'extra' in area:
        del area['extra']

for item in data['items']:
    del item['dataPath']
    if 'items' in item:
        del item['items']

newList = []
for gimmick in data['gimmicks']:
    newList.append({
        'path': gimmick
    })
data['gimmicks'] = newList

# Write to areas2.json
with open(areas2_json_path, 'w') as f:
    std_json.dump(data, f, indent=2)

print("areas2.json created successfully.")
