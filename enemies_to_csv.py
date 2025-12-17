import json
import csv

# Load the JSON data
with open('src/IntelOrca.Biohazard.BioRand.RE4R/data/enemies.json', 'r') as f:
    data = json.load(f)

# Prepare the CSV output
with open('enemies_groups.csv', 'w', newline='') as csvfile:
    writer = csv.writer(csvfile)
    writer.writerow(['key', 'groups'])
    
    for cls in data['classes']:
        key = cls['key']
        groups = ' '.join(cls['groups'])
        writer.writerow([key, groups])

print("CSV file 'enemies_groups.csv' created successfully.")