from django.shortcuts import render
from django.views import View
from django.views.generic import ListView, CreateView, UpdateView, DeleteView
from django.urls import reverse_lazy
from .models import Produto, Cliente, Empresa


class paginaInicial(View):
    def get(self, request):
        return render(request, 'precocerto/interface/home.html')

    def post(self, request):
        return render(request, 'precocerto/interface/home.html')


#def home(request):
#    produtos = Produto.objects.all()
#    return render(request, 'precocerto/home.html', {'produtos': produtos})


# Clientes

class criarCliente(CreateView):
    model = Cliente
    fields = ['nome', 'email', 'telefone']
    template_name = 'precocerto/interface/criar_cliente.html'
    success_url = reverse_lazy('home')


#def Cliente(request):
#    if request.method == 'POST':
#        nome = request.POST.get('nome')
#        email = request.POST.get('email')
#        telefone = request.POST.get('telefone')
#
#        cliente = Cliente(nome=nome, email=email, telefone=telefone)
#        cliente.save()
#
#    return render(request, 'precocerto/criar_cliente.html')

class listarClientes(ListView):
    model = Cliente
    template_name = '#'
    context_object_name = 'clientes'

# Empresas

class criarEmpresa(CreateView):
    model = Empresa
    fields = ['nome', 'cnpj', 'endereco']
    template_name = 'precocerto/interface/criar_empresa.html'
    success_url = reverse_lazy('home')

#def Empresa(request):
#    if request.method == 'POST':
#        nome = request.POST.get('nome')
#        cnpj = request.POST.get('cnpj')
#        endereco = request.POST.get('endereco')
#
#        empresa = Empresa(nome=nome, cnpj=cnpj, endereco=endereco)
#        empresa.save()
#
#    return render(request, 'precocerto/criar_empresa.html')

class listarEmpresas(ListView):
    model = Empresa
    template_name = '#'
    context_object_name = 'empresas'

# Produtos

class criarProduto(CreateView):
    model = Produto
    fields = ['nome', 'descricao', 'preco', 'imagem']
    template_name = 'precocerto/interface/criar_produto.html'
    success_url = reverse_lazy('home')

#def criar_produto(request):
#    if request.method == 'POST':
#        nome = request.POST.get('nome')
#        descricao = request.POST.get('descricao')
#        preco = request.POST.get('preco')
#        imagem = request.POST.get('imagem')
#
#        produto = Produto(nome=nome, descricao=descricao, preco=preco, imagem=imagem)
#        produto.save()
#
#    return render(request, 'precocerto/criar_produto.html')

class listarProdutos(ListView):
    model = Produto
    template_name = '#'
    context_object_name = 'produtos'

#def listar_produtos(request):
#    produtos = Produto.objects.all()
#    return render(request, '#', {'produtos': produtos})
