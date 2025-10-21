"""
Script mínimo para extrair texto e imagens de um arquivo .docx
Salva imagens em Projeto/media/extracted_images e imprime o texto no stdout.
Uso: python extract_docx.py "../media/ATUALIZADO Plano de Gerenciamento de Projeto Preço Certo.docx"
"""
import sys
import os
from docx import Document


def extract(docx_path, out_dir):
    doc = Document(docx_path)
    if not os.path.exists(out_dir):
        os.makedirs(out_dir)

    # Extrair texto
    full_text = []
    for para in doc.paragraphs:
        full_text.append(para.text)

    # Extrair imagens
    image_dir = os.path.join(out_dir, 'extracted_images')
    if not os.path.exists(image_dir):
        os.makedirs(image_dir)

    rels = doc.part._rels
    img_count = 0
    for rel in rels:
        rel_obj = rels[rel]
        if "image" in rel_obj.target_ref:
            img_count += 1
            img_name = f"image_{img_count}{os.path.splitext(rel_obj.target_ref)[1]}"
            img_path = os.path.join(image_dir, img_name)
            with open(img_path, 'wb') as f:
                f.write(rel_obj.target_part.blob)

    return "\n".join(full_text), image_dir


if __name__ == '__main__':
    if len(sys.argv) < 2:
        print("Uso: python extract_docx.py <caminho_para_docx>")
        sys.exit(1)
    docx_path = sys.argv[1]
    out_dir = os.path.dirname(docx_path)
    text, images = extract(docx_path, out_dir)
    print(text)
    print('\nImagens extraídas em:', images)
