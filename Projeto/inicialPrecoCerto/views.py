from django.shortcuts import render
from .models import Produto, Cliente, Empresa

def home(request):
    produtos = Produto.objects.all()
    return render(request, 'precocerto/home.html', {'produtos': produtos})

# Clientes

def Cliente(request):
    if request.method == 'POST':
        nome = request.POST.get('nome')
        email = request.POST.get('email')
        telefone = request.POST.get('telefone')

        cliente = Cliente(nome=nome, email=email, telefone=telefone)
        cliente.save()

    return render(request, 'precocerto/criar_cliente.html')


# Empresas

def Empresa(request):
    if request.method == 'POST':
        nome = request.POST.get('nome')
        cnpj = request.POST.get('cnpj')
        endereco = request.POST.get('endereco')

        empresa = Empresa(nome=nome, cnpj=cnpj, endereco=endereco)
        empresa.save()

    return render(request, 'precocerto/criar_empresa.html')


# Produtos

def criar_produto(request):
    if request.method == 'POST':
        nome = request.POST.get('nome')
        descricao = request.POST.get('descricao')
        preco = request.POST.get('preco')
        imagem = request.POST.get('imagem')

        produto = Produto(nome=nome, descricao=descricao, preco=preco, imagem=imagem)
        produto.save()

    return render(request, 'precocerto/criar_produto.html')

def listar_produtos(request):
    produtos = Produto.objects.all()
    return render(request, 'precocerto/listar_produtos.html', {'produtos': produtos})
