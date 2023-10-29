import sqlite3
import csv

database_file = './DataSlate.db'
csv_file = './celestial_bodies.csv'

print('Importing Celestial Bodies data...')

conn = sqlite3.connect(database_file)
cursor = conn.cursor()

data = []

# Read CSV into dictionary for more processing
with open(csv_file, 'r', newline='', encoding='utf-8') as file:
    csv_reader = csv.DictReader(file)
    for row in csv_reader:
        data.append(row)

for entry in data:
    is_moon = 1 if entry['CelestialType'] == 'Moon' else 0
    cursor.execute('INSERT INTO CelestialBodies (BodyName, IsMoon, BodyType, Gravity, Temperature, Atmosphere) VALUES (?, ?, ?, ?, ?, ?)',
            (
                entry['Name'],
                is_moon,
                entry['Type'],
                entry['Gravity'],
                entry['Temperature'],
                entry['Atmosphere']
            )
        )

    last_inserted_body_id = cursor.lastrowid

    if entry['CelestialType'] == 'Planet':
        current_planet = entry
        current_planet_id = cursor.lastrowid
        print(f'Planet: {entry["Name"]}')
    else:
        # if moon, add record into PlanetMoons
        print(f'    Moon: {entry["Name"]} of {current_planet["Name"]}')
        moon_id = cursor.lastrowid
        cursor.execute('INSERT INTO PlanetMoons (PlanetID, MoonID) VALUES (?, ?)', (current_planet_id, moon_id))

    # add celestial body into SystemBodies
    cursor.execute('SELECT SystemID FROM Systems WHERE SystemName = ?', (entry['System'],))
    system_id = cursor.fetchone()[0]
    cursor.execute('INSERT INTO SystemBodies (SystemID, BodyID) VALUES (?, ?)', (system_id, last_inserted_body_id))


conn.commit()
conn.close()

print('Celestial Bodies imported successfully!')
