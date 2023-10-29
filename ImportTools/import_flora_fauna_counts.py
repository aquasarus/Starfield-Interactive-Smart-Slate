import sqlite3
import csv

database_file = './DataSlate.db'
csv_file = './flora_fauna_fuzzy.csv'

print('Importing Flora/Fauna counts...')

conn = sqlite3.connect(database_file)
cursor = conn.cursor()

data = []

# Read CSV into dictionary for more processing
with open(csv_file, 'r', newline='', encoding='utf-8') as file:
    csv_reader = csv.DictReader(file)
    for row in csv_reader:
        data.append(row)

for entry in data:
    flora_fauna_name = entry['Name']
    body_name = entry['BodyName']
    flora_fauna_type = entry['Type']
    print(f'Processing {flora_fauna_name}({flora_fauna_type}) for {body_name}')

    # find BodyID for given Celestial Body
    cursor.execute('SELECT BodyID FROM CelestialBodies WHERE BodyName COLLATE NOCASE = ?', (body_name,))
    body_id = cursor.fetchone()[0]
    assert body_id != None

    if flora_fauna_type == 'Flora':
        type_integer = 1
        cursor.execute('UPDATE CelestialBodies SET TotalFlora = TotalFlora + 1 WHERE BodyID = ?', (body_id,))
    elif flora_fauna_type == 'Fauna':
        type_integer = 0
        cursor.execute('UPDATE CelestialBodies SET TotalFauna = TotalFauna + 1 WHERE BodyID = ?', (body_id,))
    else:
        raise Exception('Invalid Flora/Fauna type!')

    # also insert name into LifeformNames
    cursor.execute('INSERT OR IGNORE INTO LifeformNames (LifeformName, LifeformType) VALUES (?, ?)', (flora_fauna_name.title(), type_integer))


conn.commit()
conn.close()

print('Flora/Fauna counts imported successfully!')
