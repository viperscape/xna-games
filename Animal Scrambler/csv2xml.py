# csv2xml.py
# FB - 201010107
# First row of the csv file must be header!

# example CSV file: myData.csv
# id,code name,value
# 36,abc,7.6
# 40,def,3.6
# 9,ghi,6.3
# 76,def,99

import csv

csvFile = 'puzzles.txt'
xmlFile = 'puzzles.xml'

csvData = csv.reader(open(csvFile))
xmlData = open(xmlFile, 'w')
xmlData.write('<?xml version="1.0" encoding="utf-8"?>' + "\n")
xmlData.write('<XnaContent>' + "\n")
# there must be only one top-level tag
xmlData.write('<Asset Type="WindowsPhoneGame2.Puzzle[]">' + "\n")


rowNum = 0
for row in csvData:
    if rowNum == 0:
        tags = row
        # replace spaces w/ underscores in tag names
        for i in range(len(tags)):
            tags[i] = tags[i].replace(' ', '_')
    else: 
        xmlData.write('<Item>' + "\n")
        for i in range(len(tags)):
            xmlData.write('    ' + '<' + tags[i] + '>' \
                          + row[i] + '</' + tags[i] + '>' + "\n")
        xmlData.write('</Item>' + "\n")
            
    rowNum +=1

xmlData.write('</Asset>' + "\n")
#xmlData.write('</Textures>' + "\n")
xmlData.write('</XnaContent>' + "\n")
xmlData.close()
