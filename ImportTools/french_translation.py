import sqlite3
import csv
import pyperclip

database_file = './DataSlate.db'
conn = sqlite3.connect(database_file)
cursor = conn.cursor()

known_modifiers = {
    'Apex' : 'Alpha',
    'Filterer': 'Filtreur',
    'Flocking' : 'Harde',
    'Geophage' : 'Géophage',
    'Grazer': 'Brouteur',
    'Herbivore' : 'Herbivore',
    'Herding' : 'Troupeau',
    'Hunting' : 'Chasse',
    'Pack' : 'Meute',
    'Scavenger' : 'Charogne',
    'Schooling': 'Grégaire',
    'Shoaling': 'Banc',
    'Stalker': 'Dépeceur',
    'Swarming': 'Nuée',
}

# these are names that span multiple words, but don't have normal modifier schemes
known_special_names = {
    'Red Mile Mauler': 'Boxeur de la ligne rouge',
    'Apex Dust Devil Exorunner': '[Alpha] Rôdeur des sables exocoureur', # Apex and Exorunner are modifiers here
    'Rainbow Agnathan': 'Agnathan arc-en-ciel',
    'Pack Prong Wing Seabat': '[Meute] Aile dentue noctule', # Pack seems to be the only modifier here, with Prong Wing Seabat as the species name
    'Grazing Ensifer': 'Ensifer paissant',
    'Greater Jaffa Lizard': 'Grand lézard Jaffa',
    'Lesser Jaffa Lizard': 'Petit lézard Jaffa',
    'Flying Leech': 'Sangsue volante',
    'Elk Crangon': 'Orignal de Crangon',
    'Crawling Eurypterid': 'Euryptéride rampant',
    'Crag Sprinter': 'Sprinteur des falaises',
    'Baleen Rotifer': 'Rotifère baleine',
    'Terrormorph': 'Horrimorphe',

    'Cataxi Hunter': 'Chasseur Cataxi',
    'Cataxi Warrior': 'Guerrier Cataxi',
    'Grylloba Hunter': 'Chasseur Grylloba',
}

known_species_names = {
    # verbatim
    'Ashta': 'Ashta',
    'Spore' : 'Spore',
    'Vampire' : 'Vampire',
    'Dodo' : 'Dodo',
    'Cassowary' : 'Cassowary',
    'Cockatrice' : 'Cockatrice',
    'Raptor' : 'Raptor',
    'Cockroach': 'Cafard',
    'Hippodon': 'Hippodon',
    'Dragon': 'Dragon',
    'Scorpion': 'Scorpion',
    'Manta' : 'Manta',
    'Mantodea': 'Mantodea',
    'Puffball' : 'Puffball',
    'Morion': 'Morion',
    'Drone': 'Drone',
    'Pearl': 'Pearl',
    'Arachnomantis': 'Arachnomantis',
    'Cricket': 'Cricket',
    'Trident': 'Trident',
    'Cyclozard': 'Cyclozard',
    'Olgreg': 'Olgreg',
    'Geckon': 'Geckon',
    'Arapaima': 'Arapaima',
    'Ikuradon': 'Ikuradon',
    'Dragondon': 'Dragondon',
    'Trilobite': 'Trilobite',
    'Metropus': 'Metropus',
    'Crocalypse': 'Crocalypse',

    # translation match
    'Hammersaur': 'Martosaure',
    'Mossasaur': 'Mossasaure',
    'Kronosaurus' : 'Kronosaure',

    'Coralcrab': 'Crabe corail',
    'Crab': 'Crabe',
    'Orchid' : 'Orchidée',
    'Ghost' : 'Fantôme',
    'Seahorse' : 'Hippocampe',

    'Longhorn' : 'Long-corne',
    'Bighorn': 'Grand-corne',
    'Trihorn': 'Tricorne',

    'Monitor' : 'Moniteur',
    'Nightmare' : 'Cauchemar',
    'Sentinel' : 'Sentinelle',

    'Beetle': 'Scarabée',
    'Carasnail': 'Carascargot',
    'Snail': 'Escargot',
    'Worm': 'Ver',
    'Tapeworm': 'Ténia',
    'Triworm': 'Trivers',
    'Caterpillar': 'Chenille',
    'Arachnofly': 'Arachnomouche',
    'Roundshell': 'Carapace ronde',
    'Centiskull': 'Centicrâne',
    'Spiderwasp': 'Guêpe-araignée',

    'Glider': 'Planeur',
    'Vuvuzelisk': 'Vuvuzelisque',

    'Blistercrab': 'Crabe cloqué',
    'Crabfly': 'Mouche-crabe',
    'Beetlecrab': 'Coléocrabe',

    'Coralbucket': 'Pot corail',
    'Coralcrawler': 'Rampe-corail',
    'Coralheart': 'Cœur de corail',
    'Coralbug': 'Mante corail',
    
    'Thorn': 'Épine',
    'Thornmantis': 'Mante épineuse',

    'Lionfish': 'Poisson-lion',
    'Lurefish': 'Poisson-appât',
    'Milliwhale': 'Millibaleine',
    'Exowhale': 'Exobaleine',
    'Cephalopod': 'Céphalopode',
    'Unishark': 'Unirequin',

    'Eggback': 'Porteur d\'œufs',
    'Eggsac': 'Sac d\'œufs',
    'Eggtail' : 'Croupœuf',

    'Scepter': 'Sceptre',
    'Artichoke': 'Artichaut',
    'Pinecone': 'Pomme de pin',
    'Exorunner': 'Exocoureur',
    'Sloth': 'Paresseux',
    'Vulture': 'Vautour',
    'Sunflower': 'Tournesol',

    # maybe
    'Ankylosaurus': 'Ankylosaure',
    'Octomaggot': 'Octovers',

    'Leafbug': 'Phyllie',
    'Exocrawler': 'Exobestiole',
    'Leafback': 'Marcherécif',

    'Tuskfrog': 'Crapaud-croc',

    'Spikeworm': 'Ver-épic',

    'Chasmbass': 'Perche abyssale',

    'Bonemane': 'Crin d\'os',
    'Boneshell': 'Huître-os',

    'Jackknife': 'Couteau',

    'Flamethorn': 'Cornu ardent',
    'Parrothawk': 'Perroquet-buse',
    'Angler': 'Lophius',
    'Brainsquid': 'Céphalocalmar',
    'Longarm': 'Granbras',
    'Crocodaunt': 'Crocofrayant',
    'Lockjaw': 'Serre-gueule',
    'Anteater': 'Fourmivore',

    # not sure
    'Thornback' : 'Échinoserre',

    'Whaleshark': 'Requin-baleine',
    'Sharkwhale': 'Baleine-requin',

    'Nautilus' : 'Nautilus',
    'Nautiloos': 'Nautile',
}

fauna_names = []
cursor.execute('SELECT LifeformName FROM LifeformNames WHERE LifeformType = ?', (0,))
rows = cursor.fetchall()

# word_count = {}
# for row in rows:
#     lifeform_name = row[0]

#     if lifeform_name in known_special_names:
#         continue

#     split_name = lifeform_name.split()

#     for word in split_name:
#         if word in word_count:
#             word_count[word] += 1
#         else:
#             word_count[word] = 1

# sorted_items = sorted(word_count.items(), key=lambda x: x[1], reverse=True)

# potential_species = ''
# for key, value in sorted_items:
#     if key not in known_modifiers and key not in known_species_names:
#         # potential_species += f'{key}: {value}\n'
#         potential_species += f'{key}\n'

# print(potential_species)
# pyperclip.copy(potential_species)

# print(f'\nTotal words: {len(sorted_items)}')
# print(f'Total modifiers: {len(known_modifiers)}')
# print(f'Current species count: {len(sorted_items) - len(known_modifiers)}')


translation = {}
output = ''
for row in rows:
    lifeform_name = row[0]

    if lifeform_name in known_special_names:
        # translate known special names (hardcoded matches)
        if known_special_names[lifeform_name] != '':
            translation[lifeform_name] = known_special_names[lifeform_name]
            print(f'Translated: {lifeform_name}  ->  {translation[lifeform_name]}')
            output += f'{translation[lifeform_name]}\n'
    else:
        split_name = lifeform_name.split()
        modified_split_name = ''

        # translate all modifiers, if found
        for word in split_name:
            if word in known_modifiers:
                modified_split_name += f' [{known_modifiers[word]}]'
            else:
                modified_split_name += f' {word}'

        modified_split_name = modified_split_name.lstrip()

        # translate species name
        for word in modified_split_name.split():
            species_name_count = 0
            if not word.startswith('['):
                species_name_count += 1
                species_name = word

        if species_name_count > 1:
            print(f'[ERROR] Failed to parse species name: {modified_split_name}')
            sys.exit()

        if species_name in known_species_names:
            modified_split_name = modified_split_name.replace(species_name, known_species_names[species_name])

            # only accept fully translated names
            translation[lifeform_name] = modified_split_name
            print(f'Translated: {lifeform_name}  ->  {translation[lifeform_name]}')
            output += f'{translation[lifeform_name]}\n'

print(f'Total English lifeforms: {len(rows)}')
print(f'Total translated French lifeforms: {len(translation)}')

# copy to clipboard
pyperclip.copy(output)

conn.commit()
conn.close()
