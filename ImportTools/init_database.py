import sqlite3
import os
import subprocess
import shutil
from pathlib import Path

db_path = './DataSlate.db'

# Check if the file exists
if os.path.exists(db_path):
    # Delete the file
    os.remove(db_path)
    print(f'File "{db_path}" has been deleted.')
else:
    print(f'File "{db_path}" does not exist.')

# Connect to the database (it will be created if it doesn't exist)
conn = sqlite3.connect(db_path)
cursor = conn.cursor()

# Read and execute SQL commands from the .sql file
print('Creating database tables...')
with open('create_tables.sql', 'r') as sql_file:
    sql_commands = sql_file.read()
    cursor.executescript(sql_commands)

# Commit changes and close the connection
conn.commit()
conn.close()

print('Database created!')

subprocess.run(['python', './import_systems.py'], check=True)
subprocess.run(['python', './import_resources.py'], check=True)
subprocess.run(['python', './import_celestial_bodies.py'], check=True)
subprocess.run(['python', './import_celestial_bodies_metadata.py'], check=True)
subprocess.run(['python', './import_body_resources.py'], check=True)
subprocess.run(['python', './import_flora_fauna_counts.py'], check=True)
# subprocess.run(['python', './add_sample_data.py'], check=True)

parent_path = os.path.abspath(os.path.join(db_path, os.pardir))
parent_of_parent = os.path.abspath(os.path.join(parent_path, os.pardir))
destination = os.path.join(parent_of_parent, 'Database')
print(f'Copying to project directory {destination}')
shutil.copy2(db_path, destination)
