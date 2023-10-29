import sqlite3
import csv

database_file = './DataSlate.db'
csv_file = './body_resources.csv'

print('Importing Body Resources data...')

conn = sqlite3.connect(database_file)
cursor = conn.cursor()

data = []

# Read CSV into dictionary for more processing
with open(csv_file, 'r', newline='', encoding='utf-8') as file:
    csv_reader = csv.DictReader(file)
    for row in csv_reader:
        data.append(row)

# use a dummy row in raw data to fetch all ResourceIDs
resources = {}
dummy_entry = data[0]
for key, value in dummy_entry.items():
    if key == 'BodyName':
        continue
    
    cursor.execute('SELECT ResourceID FROM Resources WHERE ResourceShortName = ?', (key,))
    resource_id = cursor.fetchone()[0]
    assert resource_id != None
    resources[key] = resource_id

print(f'Resource keys: {resources}')

for entry in data:
    body_name = entry['BodyName']
    print(f'Processing resources for {body_name}')

    # find BodyID for given Celestial Body
    cursor.execute('SELECT BodyID FROM CelestialBodies WHERE BodyName = ?', (body_name,))
    body_id = cursor.fetchone()[0]
    assert body_id != None

    for key, value in entry.items():
        if value != 'X':
            continue

        print(f'{body_name} has {key}')
        resource_id = resources[key]
        cursor.execute('INSERT INTO BodyResources (BodyID, ResourceID) VALUES (?, ?)', (body_id, resource_id))


conn.commit()
conn.close()

print('Body Resources imported successfully!')
