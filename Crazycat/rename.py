from glob import glob
from os import rename
tag = "wolf_attack"
ntag = "attack"
for fname in glob(tag+'*.png'):
    rename(fname, ntag + fname[len(tag):])
