from django.shortcuts import render
from django.views import View
from django.views.generic import ListView, CreateView, UpdateView, DeleteView
from django.urls import reverse_lazy
from django.contrib.auth.models import User
from django.contrib.auth import authenticate, login
from .models import Produto, Cliente, Empresa


# Pagina Inicial

class paginaInicial(View):
    def get(self, request):
        return render(request, 'precocerto/interface/home.html')

    def post(self, request):
        return render(request, 'precocerto/interface/home.html')

# Clientes

class criarCliente(CreateView):
    model = Cliente
    fields = ['nome', 'usuario', 'email', 'telefone']
    template_name = 'precocerto/interface/criar_cliente.html'
    success_url = reverse_lazy('home')

    def get_context_data(self, **kwargs):
        context = super().get_context_data(**kwargs)
        context['senha_field'] = True
        return context
    
    def post(self, request, *args, **kwargs):
        nome =  request.POST.get('nome')
        usuario = request.POST.get('usuario')
        email = request.POST.get('email')
        senha = request.POST.get('senha')

        if User.objects.filter(username=usuario).exists():
            return render(request, self.template_name, {
                'form': self.get_form(),
                'error': 'Nome de usuário já existe. Escolha outro.'
            })

        usuario = User.objects.create_user(
            username=usuario,
            password=senha,
            email=email,
            first_name=nome
        )


        Cliente.objects.create(
            nome=nome,
            usuario=usuario,
            email=email,
        )
        return render(request, 'precocerto/interface/home.html', {'message': 'Cliente criado com sucesso!'})

class logarCliente(View):
    def get(self, request):
        return render(request, 'precocerto/cliente/logarCliente.html')
    
    def post(self, request):
        usuario = request.POST.get('usuario')
        senha = request.POST.get('senha')
        user = authenticate(request, username=usuario, password=senha)

        if user is not None:
            login(request, user)
            return render(request, 'precocerto/interface/home.html', {'message': 'Login realizado com sucesso!'})
        else:
            return render(request, 'precocerto/cliente/logarCliente.html', {'error': 'Usuário ou senha inválidos.'})

# Empresas

class criarEmpresa(CreateView):
    model = Empresa
    fields = ['nome', 'cnpj', 'endereco']
    template_name = 'precocerto/interface/criar_empresa.html'
    success_url = reverse_lazy('home')

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

class listarProdutos(ListView):
    model = Produto
    template_name = '#'
    context_object_name = 'produtos'