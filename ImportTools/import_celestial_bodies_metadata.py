import sqlite3
import csv

database_file = './DataSlate.db'
csv_file = './celestial_bodies_metadata.csv'

print('Importing additional Celestial Bodies metadata...')

conn = sqlite3.connect(database_file)
cursor = conn.cursor()

data = []

# Read CSV into dictionary for more processing
with open(csv_file, 'r', newline='', encoding='utf-8') as file:
    csv_reader = csv.DictReader(file)
    for row in csv_reader:
        data.append(row)

for entry in data:
    # find BodyID for given Celestial Body
    cursor.execute('SELECT BodyID FROM CelestialBodies WHERE BodyName = ?', (entry['BodyName'],))
    body_id = cursor.fetchone()[0]
    assert body_id != None

    print(f'Adding metadata to {entry["BodyName"]} ({body_id})')
    cursor.execute('UPDATE CelestialBodies SET Magnetosphere = ?, Water = ? WHERE BodyID = ?', (entry['Magnetosphere'], entry['Water'], body_id))

conn.commit()
conn.close()

print('Celestial Bodies metadata imported successfully!')
