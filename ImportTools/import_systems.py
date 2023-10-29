import sqlite3
import csv

# Path to your SQLite database file
database_file = './DataSlate.db'

# Path to your CSV file
csv_file = './systems.csv'

print('Importing Systems data...')

# Connect to the SQLite database
conn = sqlite3.connect(database_file)
cursor = conn.cursor()

# Read data from CSV and insert into the database
with open(csv_file, 'r', newline='', encoding='utf-8') as file:
    csv_reader = csv.reader(file)
    next(csv_reader)  # Skip header row if present in the CSV
    for row in csv_reader:
        # Assuming the CSV columns are in order: column1, column2, ..., columnN
        # Adjust the SQL query and the row index accordingly based on your CSV structure
        cursor.execute('INSERT INTO Systems (SystemName, SystemLevel) VALUES (?, ?)', row)

# Commit changes and close the connection
conn.commit()
conn.close()

print('Systems imported successfully!')
