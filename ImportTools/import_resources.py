import sqlite3
import csv

# Path to your SQLite database file
database_file = './DataSlate.db'

# Path to your CSV file
csv_file = './resources.csv'

print('Importing Resources data...')

# Connect to the SQLite database
conn = sqlite3.connect(database_file)
cursor = conn.cursor()

# Read data from CSV and insert into the database
with open(csv_file, 'r', newline='', encoding='utf-8') as file:
    csv_reader = csv.reader(file)
    next(csv_reader)  # Skip header row if present in the CSV
    for row in csv_reader:
        processed_row = (row[0], row[1], None if row[2] == '' else row[2], row[3])
        print(processed_row)
        # Assuming the CSV columns are in order: column1, column2, ..., columnN
        # Adjust the SQL query and the row index accordingly based on your CSV structure
        cursor.execute('INSERT INTO Resources (ResourceName, ResourceType, ResourceShortName, ResourceRarity) VALUES (?, ?, ?, ?)', processed_row)

# Commit changes and close the connection
conn.commit()
conn.close()

print('Resources imported successfully!')
