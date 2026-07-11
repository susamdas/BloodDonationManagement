from bs4 import BeautifulSoup
import pathlib

p = pathlib.Path('temp.html')
html = p.read_text('utf-8')
soup = BeautifulSoup(html, 'html.parser')
rows = []
for table in soup.select('table.wikitable'):
    for tr in table.select('tr')[1:]:
        cells = [td.get_text(strip=True) for td in tr.find_all(['th', 'td'])]
        if len(cells) < 2:
            continue
        district = cells[0].replace(' District', '').strip()
        upazila_text = cells[1].strip()
        parts = [part.strip() for part in upazila_text.split('Upazila') if part.strip()]
        for part in parts:
            name = part.rstrip(',').strip()
            if name:
                rows.append((district, name))

# preserve first seen district order
seen = set()
unique_districts = []
for district, _ in rows:
    if district not in seen:
        seen.add(district)
        unique_districts.append(district)

if len(unique_districts) != len(set(unique_districts)):
    raise SystemExit('Duplicate districts found')

# Map district to identity order
district_id = {district: i + 1 for i, district in enumerate(unique_districts)}

sql_lines = ["SET NOCOUNT ON;\n"]
for district in unique_districts:
    name = district.replace("'", "''")
    sql_lines.append(f"INSERT INTO Districts (Name) VALUES (N'{name}');")

sql_lines.append('\n')
for district, thana in rows:
    district_fk = district_id[district]
    name = thana.replace("'", "''")
    sql_lines.append(f"INSERT INTO Thanas (Name, DistrictId) VALUES (N'{name}', {district_fk});")

out = pathlib.Path('seed_bangladesh_district_thana.sql')
out.write_text('\n'.join(sql_lines), 'utf-8')
print(f'Districts: {len(unique_districts)}')
print(f'Thanas/Upazilas: {len(rows)}')
print('SQL file generated:', out)
