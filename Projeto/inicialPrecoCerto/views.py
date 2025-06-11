from django.shortcuts import render, redirect
from django.views import View
from django.views.generic import ListView, CreateView, UpdateView, DeleteView, DetailView
from django.urls import reverse_lazy
from django.contrib.auth.models import User
from django.contrib.auth import authenticate, login
from .models import Produto, Cliente, Empresa
from django.contrib.auth.mixins import LoginRequiredMixin, UserPassesTestMixin
from django.contrib.auth import logout


# Pagina Inicial

class paginaInicial(View):
    def get(self, request):
        produtos = Produto.objects.all()
        usuario_empresa = None
        usuario_cliente = None
        produtos_empresa = None
        if request.user.is_authenticated:
            try:
                usuario_empresa = Empresa.objects.get(usuario=request.user)
                produtos_empresa = Produto.objects.filter(empresa=usuario_empresa)
            except Empresa.DoesNotExist:
                try:
                    usuario_cliente = Cliente.objects.get(usuario=request.user)
                except Cliente.DoesNotExist:
                    pass
        return render(request, 'precocerto/interface/home.html', {
            'produtos': produtos,
            'usuario_empresa': usuario_empresa,
            'usuario_cliente': usuario_cliente,
            'produtos_empresa': produtos_empresa
        })

    def post(self, request):
        produtos = Produto.objects.all()
        usuario_empresa = None
        usuario_cliente = None
        produtos_empresa = None
        if request.user.is_authenticated:
            try:
                usuario_empresa = Empresa.objects.get(usuario=request.user)
                produtos_empresa = Produto.objects.filter(empresa=usuario_empresa)
            except Empresa.DoesNotExist:
                try:
                    usuario_cliente = Cliente.objects.get(usuario=request.user)
                except Cliente.DoesNotExist:
                    pass
        return render(request, 'precocerto/interface/home.html', {
            'produtos': produtos,
            'usuario_empresa': usuario_empresa,
            'usuario_cliente': usuario_cliente,
            'produtos_empresa': produtos_empresa
        })

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
        telefone = request.POST.get('telefone')

        if User.objects.filter(username=usuario).exists():
            user = User.objects.get(username=usuario)
            # Garante que o cliente seja criado se não existir
            if not Cliente.objects.filter(usuario=user).exists():
                Cliente.objects.create(
                    nome=nome,
                    usuario=user,
                    email=email,
                    telefone=telefone
                )
            return render(request, self.template_name, {
                'form': self.get_form(),
                'error': 'Nome de usuário já existe. Escolha outro.'
            })

        user = User.objects.create_user(
            username=usuario,
            password=senha,
            email=email,
            first_name=nome
        )

        Cliente.objects.create(
            nome=nome,
            usuario=user,
            email=email,
            telefone=telefone
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
            return redirect('home')
        else:
            return render(request, 'precocerto/cliente/logarCliente.html', {'error': 'Usuário ou senha inválidos.'})
        


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
        nome = usuario  # nome da empresa igual ao usuário

        if User.objects.filter(username=usuario).exists():
            return render(request, self.template_name, {
                'error': 'Nome de usuário já existe. Escolha outro.'
            })

        user = User.objects.create_user(
            username=usuario,
            password=senha,
            first_name=nome
        )

        Empresa.objects.create(
            nome=nome,
            usuario=user,
            cnpj=cnpj,
            endereco=endereco
        )
        return render(request, 'precocerto/interface/home.html', {'message': 'Empresa criada com sucesso!'})
    

class deletarEmpresa(DeleteView):
    model = Empresa
    template_name = 'precocerto/empresa/deletar_empresa.html'
    success_url = reverse_lazy('home')

    def delete(self, request, *args, **kwargs):
        self.object = self.get_object()
        usuario = self.object.usuario
        response = super().delete(request, *args, **kwargs)
        if usuario:
            usuario.delete()
        return response


class logarEmpresa(View):
    def get(self, request):
        return render(request, 'precocerto/empresa/logarEmpresa.html')
    
    def post(self, request):
        usuario = request.POST.get('usuario')
        senha = request.POST.get('senha')
        user = authenticate(request, username=usuario, password=senha)

        if user is not None:
            try:
                empresa = Empresa.objects.get(usuario=user)
                login(request, user)
                return redirect('home')
            except Empresa.DoesNotExist:
                pass
        return render(request, 'precocerto/empresa/logarEmpresa.html', {'error': 'Usuário ou senha inválidos.'})


class logoutEmpresa(View):
    def get(self, request):
        logout(request)
        return render(request, 'precocerto/empresa/logoutEmpresa.html')

    def post(self, request):
        logout(request)
        return render(request, 'precocerto/empresa/logoutEmpresa.html')

class perfilEmpresa(ListView):
    model = Empresa
    template_name = 'precocerto/empresa/perfilEmpresa.html'
    context_object_name = 'empresas'


class editarEmpresa(LoginRequiredMixin, UserPassesTestMixin, UpdateView):
    model = Empresa
    fields = ['nome', 'cnpj', 'endereco']
    template_name = 'precocerto/empresa/editar_empresa.html'
    success_url = reverse_lazy('home')

    def test_func(self):
        empresa = self.get_object()
        return empresa.usuario == self.request.user
    
    def form_valid(self, form):
        response = super().form_valid(form)
        empresa = self.object
        usuario = empresa.usuario

        novo_nome = empresa.nome  # O nome da empresa vindo do formulário

        # Verifica se já existe outro usuário com esse username
        if User.objects.filter(username=novo_nome).exclude(pk=usuario.pk).exists():
            form.add_error(None, "Nome de usuário já existe.")
            return self.form_invalid(form)

        usuario.first_name = novo_nome
        usuario.username = novo_nome
        usuario.save()
        return response

# Produtos

    # vincular o produto a empresa
class criarProduto(CreateView):
    model = Produto
    fields = ['nome', 'descricao', 'preco', 'imagem']
    template_name = 'precocerto/produto/criar_produto.html'
    success_url = reverse_lazy('home')

    def form_valid(self, form):
        # Vincula o produto à empresa logada
        if self.request.user.is_authenticated:
            try:
                empresa = Empresa.objects.get(usuario=self.request.user)
                form.instance.empresa = empresa
            except Empresa.DoesNotExist:
                pass
        return super().form_valid(form)

class detalheProduto(DetailView):
    model = Produto
    template_name = 'precocerto/produto/detalheProduto.html'
    context_object_name = 'produto'

class editarProduto(LoginRequiredMixin, UserPassesTestMixin, UpdateView):
    model = Produto
    fields = ['nome', 'descricao', 'preco', 'imagem'
    ]
    template_name = 'precocerto/produto/editar_produto.html'
    success_url = reverse_lazy('home')

    def test_func(self):
        produto = self.get_object()
        try:
            empresa = Empresa.objects.get(usuario=self.request.user)
            return produto.empresa == empresa
        except Empresa.DoesNotExist:
            return False
        
    def form_valid(self, form):
        # Vincula o produto à empresa logada
        if self.request.user.is_authenticated:
            try:
                empresa = Empresa.objects.get(usuario=self.request.user)
                form.instance.empresa = empresa
            except Empresa.DoesNotExist:
                pass
        return super().form_valid(form)

class deletarProduto(LoginRequiredMixin, UserPassesTestMixin, DeleteView):
    model = Produto
    template_name = 'precocerto/produto/deletar_produto.html'
    success_url = reverse_lazy('home')

    def test_func(self):
        produto = self.get_object()
        try:
            empresa = Empresa.objects.get(usuario=self.request.user)
            return produto.empresa == empresa
        except Empresa.DoesNotExist:
            return False
        
    def form_valid(self, form):
    # Vincula o produto à empresa logada
        if self.request.user.is_authenticated:
            try:
                empresa = Empresa.objects.get(usuario=self.request.user)
                form.instance.empresa = empresa
            except Empresa.DoesNotExist:
                pass
        return super().form_valid(form)

# Carrinho
class adicionarCarrinho(View):
    def post(self, request, produto_id):
        if not request.user.is_authenticated:
            return redirect('logar_cliente')
        produto = Produto.objects.get(id=produto_id)
        carrinho = request.session.get('carrinho', {})
        if str(produto_id) in carrinho:
            carrinho[str(produto_id)]['quantidade'] += 1
        else:
            carrinho[str(produto_id)] = {
                'nome': produto.nome,
                'preco': float(produto.preco),
                'imagem': produto.imagem.url if produto.imagem else '',
                'quantidade': 1
            }
        request.session['carrinho'] = carrinho
        return redirect('ver_carrinho')

class verCarrinho(View):
    def get(self, request):
        if not request.user.is_authenticated:
            return redirect('logar_cliente')
        carrinho = request.session.get('carrinho', {})
        total = sum(item['preco'] * item['quantidade'] for item in carrinho.values())
        return render(request, 'precocerto/cliente/carrinho.html', {'carrinho': carrinho, 'total': total})

class alterarQuantidade(View):
    def post(self, request, produto_id):
        if not request.user.is_authenticated:
            return redirect('ver_carrinho')
        nova_qtd = int(request.POST.get('quantidade', 1))
        carrinho = request.session.get('carrinho', {})
        if str(produto_id) in carrinho:
            if nova_qtd > 0:
                carrinho[str(produto_id)]['quantidade'] = nova_qtd
            else:
                del carrinho[str(produto_id)]
        request.session['carrinho'] = carrinho
        return redirect('ver_carrinho')

class removerCarrinho(View):
    def post(self, request, produto_id):
        if not request.user.is_authenticated:
            return redirect('ver_carrinho')
        carrinho = request.session.get('carrinho', {})
        if str(produto_id) in carrinho:
            del carrinho[str(produto_id)]
        request.session['carrinho'] = carrinho
        return redirect('ver_carrinho')

class confirmarCompra(View):
    def post(self, request):
        if not request.user.is_authenticated:
            return redirect('logar_cliente')
        request.session['carrinho'] = {}
        return render(request, 'precocerto/cliente/carrinho.html', {'carrinho': {}, 'total': 0, 'mensagem': 'Compra confirmada com sucesso!'})