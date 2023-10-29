import sqlite3
import csv

database_file = './DataSlate.db'

print('Adding sample data...')

conn = sqlite3.connect(database_file)
cursor = conn.cursor()

# find Montara Luna
cursor.execute('SELECT BodyID FROM CelestialBodies WHERE BodyName = \'Montara Luna\'')
montara_luna_id = cursor.fetchone()[0]
assert montara_luna_id != None

# find Amino Acids
cursor.execute('SELECT ResourceID FROM Resources WHERE ResourceName = \'Amino Acids\'')
amino_acids_id = cursor.fetchone()[0]
assert amino_acids_id != None

# add Faunas
cursor.execute('INSERT INTO Faunas (FaunaName, ParentBodyID) VALUES (\'Centiskull Grazer\', ?)', (montara_luna_id,))
cursor.execute('INSERT INTO Faunas (FaunaName, ParentBodyID) VALUES (\'Flocking Dodo Grazer\', ?)', (montara_luna_id,))
cursor.execute('INSERT INTO Faunas (FaunaName, ParentBodyID) VALUES (\'Flocking Horsamander Grazer\', ?)', (montara_luna_id,))
cursor.execute('INSERT INTO Faunas (FaunaName, ParentBodyID) VALUES (\'Herding Cockatrice Herbivore\', ?)', (montara_luna_id,))

# more details for Pack Octomaggot
cursor.execute('INSERT INTO Faunas (FaunaName, ParentBodyID) VALUES (\'Pack Octomaggot\', ?)', (montara_luna_id,))
pack_octomaggot_id = cursor.lastrowid
cursor.execute('INSERT INTO FaunaResources (FaunaID, ResourceID, DropRate) VALUES (?, ?, 0)', (pack_octomaggot_id, amino_acids_id))
notes = 'Looks disgusting.\nSome details:\n- Wary\n- Found in Hills, Volcanic, Frozen Dunes\n- 150 HP'
cursor.execute('UPDATE Faunas SET FaunaNotes = ? WHERE FaunaID = ?', (notes, pack_octomaggot_id))

cursor.execute('INSERT INTO Faunas (FaunaName, ParentBodyID) VALUES (\'Schooling Featherfin Filterer\', ?)', (montara_luna_id,))

# add Floras
cursor.execute('INSERT INTO Floras (FloraName, ParentBodyID) VALUES (\'Blooming Slopefeather\', ?)', (montara_luna_id,))
cursor.execute('INSERT INTO Floras (FloraName, ParentBodyID) VALUES (\'Crag Root\', ?)', (montara_luna_id,))
cursor.execute('INSERT INTO Floras (FloraName, ParentBodyID) VALUES (\'Golden Creeper\', ?)', (montara_luna_id,))
cursor.execute('INSERT INTO Floras (FloraName, ParentBodyID) VALUES (\'Savage Woundwort\', ?)', (montara_luna_id,))
cursor.execute('INSERT INTO Floras (FloraName, ParentBodyID) VALUES (\'Savanna Sweetbush\', ?)', (montara_luna_id,))
cursor.execute('INSERT INTO Floras (FloraName, ParentBodyID) VALUES (\'Sweet Canis Vine\', ?)', (montara_luna_id,))
cursor.execute('INSERT INTO Floras (FloraName, ParentBodyID) VALUES (\'Twilight Cradleleaf\', ?)', (montara_luna_id,))

# discover systems
cursor.execute('UPDATE Systems SET Discovered = 1 WHERE SystemName IN (\'Cheyenne\', \'Alpha Centauri\')')

conn.commit()
conn.close()

print('Sample data added successfully!')
