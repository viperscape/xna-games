from PIL import Image
import glob,os

fold = "sprites\\wolf\\"

for infile in glob.glob(fold+"*.bmp"):
    file, ext = os.path.splitext(infile)
    im = Image.open(infile)
    name = file;
    im.save(name.replace(" ","_") + ".png", "PNG")

