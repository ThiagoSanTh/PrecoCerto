from django.shortcuts import render
from django.views import View
from django.views.generic import ListView, CreateView, UpdateView, DeleteView, DetailView
from django.urls import reverse_lazy
from django.contrib.auth.models import User
from django.contrib.auth import authenticate, login
from .models import Produto, Cliente, Empresa


# Pagina Inicial

class paginaInicial(View):
    def get(self, request):
        produtos = Produto.objects.all()
        return render(request, 'precocerto/interface/home.html', {'produtos': produtos})

    def post(self, request):
        produtos = Produto.objects.all()
        return render(request, 'precocerto/interface/home.html', {'produtos': produtos})

# Clientes

class criarCliente(CreateView):
    model = Cliente
    fields = ['nome', 'usuario', 'email', 'telefone']
    template_name = 'precocerto/cliente/criar_cliente.html'
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
        

    #corrigir 
class perfilCliente(ListView):
    model = Cliente
    template_name = 'precocerto/cliente/perfilCliente.html'
    context_object_name = 'clientes'

# Empresas

    #corrigir
class criarEmpresa(CreateView):
    template_name = 'precocerto/empresa/criar_empresa.html'

    def get(self, request):
        return render(request, self.template_name)

    def post(self, request):
        usuario = request.POST.get('usuario')
        cnpj = request.POST.get('cnpj')
        endereco = request.POST.get('endereco')
        senha = request.POST.get('senha')

        if User.objects.filter(username=usuario).exists():
            return render(request, self.template_name, {
                'error': 'Nome de usuário já existe. Escolha outro.'
            })

        usuario = User.objects.create_user(
            username=usuario,
            password=senha,
            first_name=usuario
        )

        Empresa.objects.create(
            usuario=usuario,
            cnpj=cnpj,
            endereco=endereco
        )
        return render(request, 'precocerto/interface/home.html', {'message': 'Empresa criada com sucesso!'})
    
    #corrigir
class logarEmpresa(View):
    def get(self, request):
        return render(request, 'precocerto/empresa/logarEmpresa.html')
    
    def post(self, request):
        cnpj = request.POST.get('cnpj')
        senha = request.POST.get('senha')
        usuario = authenticate(request, username=usuario, password=senha)

        if usuario is not None and hasattr(usuario, 'empresa'):
            login(request, usuario)
            return render(request, 'precocerto/interface/home.html', {'message': 'Login realizado com sucesso!'})
        else:
            return render(request, 'precocerto/empresa/logarEmpresa.html', {'error': 'CNPJ ou senha inválidos.'})

    #corrigir
class perfilEmpresa(ListView):
    model = Empresa
    template_name = 'precocerto/empresa/perfilEmpresa.html'
    context_object_name = 'empresas'



# Produtos

    # vincular o produto a empresa
class criarProduto(CreateView):
    model = Produto
    fields = ['nome', 'descricao', 'preco', 'imagem']
    template_name = 'precocerto/produto/criar_produto.html'
    success_url = reverse_lazy('home')

class detalheProduto(DetailView):
    model = Produto
    template_name = 'precocerto/produto/detalheProduto.html'
    context_object_name = 'produto'
