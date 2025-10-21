from PIL import Image
import os
p=r'c:\Users\thiag\Desktop\Faculdade\PrecoCerto\Projeto\media\extracted_images'
files=sorted(os.listdir(p))
for f in files:
    fp=os.path.join(p,f)
    with Image.open(fp) as im:
        print(f, im.format, im.size, im.mode)
