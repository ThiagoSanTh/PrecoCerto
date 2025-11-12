from django.shortcuts import render, redirect
from django.views import View
from django.db.models import Q
from .models import Produto, Cliente, Empresa
from django.contrib.auth.models import User
from .forms import RegistroForm
from django.contrib.auth import get_user_model
from django.contrib import messages
from django.contrib import messages
from django.contrib.auth import authenticate, login, logout
from django.urls import reverse, reverse_lazy
from django.views.generic import ListView, CreateView, UpdateView, DeleteView, DetailView
from django.contrib.auth.mixins import LoginRequiredMixin, UserPassesTestMixin


# Interface


class register(View):
    """View de registro unificada que usa `RegistroForm`.

    - GET: exibe o formulário
    - POST: valida, cria Cliente ou Empresa via form.save(), autentica e faz login
    """
    template_name = 'precocerto/interface/register.html'

    def get(self, request):
        # allow optional preset of tipo via URL kwargs or querystring (e.g. ?tipo=empresa)
        tipo = request.GET.get('tipo')
        # If URL dispatcher passes a tipo kwarg, it will be in request.resolver_match.kwargs
        if not tipo:
            tipo = request.resolver_match.kwargs.get('tipo') if request.resolver_match else None

        initial = {}
        if tipo in ('cliente', 'empresa'):
            initial['tipo'] = tipo

        form = RegistroForm(initial=initial)
        return render(request, self.template_name, {'form': form})

    def post(self, request):
        form = RegistroForm(request.POST)
        if form.is_valid():
            obj = form.save()
            # `obj` é Cliente ou Empresa; ambos têm o campo `usuario` (User)
            user = obj.usuario

            # Autentica pelo username e senha fornecidos (mais seguro/compatível que setar backend diretamente)
            username = user.username
            senha = request.POST.get('senha')
            user_auth = authenticate(request, username=username, password=senha)
            if user_auth is not None:
                login(request, user_auth)
                messages.success(request, 'Registro realizado com sucesso.')
                return redirect('home')
            else:
                # Caso raro: o usuário foi criado mas não conseguiu autenticar; faz login manualmente como fallback
                try:
                    user.backend = 'django.contrib.auth.backends.ModelBackend'
                    login(request, user)
                    messages.warning(request, 'Usuário criado, login efetuado via fallback.')
                    return redirect('home')
                except Exception:
                    messages.error(request, 'Erro ao realizar login automático. Faça login manualmente.')
                    return redirect(reverse('logar') + '?tipo=cliente')

        # Se inválido, reexibe o form com erros
        return render(request, self.template_name, {'form': form})


# Página Inicial
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
        termo = request.POST.get('search', '')  # pega o valor do input name="search"
        produtos = Produto.objects.filter(
            Q(nome__icontains=termo) | Q(descricao__icontains=termo)
        )

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

class logar(View):
    """Unified login view for Cliente and Empresa.

    - GET: renderiza template `precocerto/interface/login.html` (usa query param `tipo` para exibir texto)
    - POST: tenta autenticar por username ou email, determina se o user é Empresa ou Cliente e realiza login
    """
    template_name = 'precocerto/interface/login.html'

    def get(self, request):
        tipo = request.GET.get('tipo', 'cliente')
        return render(request, self.template_name, {'tipo': tipo})

    def post(self, request):
        usuario_input = request.POST.get('usuario')
        senha = request.POST.get('senha')

        # Tenta autenticar diretamente por username
        user = authenticate(request, username=usuario_input, password=senha)
        if user is None:
            # tenta localizar por email e autenticar pelo username
            UserModel = get_user_model()
            try:
                user_obj = UserModel.objects.get(email=usuario_input)
                user = authenticate(request, username=user_obj.username, password=senha)
            except UserModel.DoesNotExist:
                user = None

        if user is None:
            messages.error(request, 'Usuário ou senha inválidos.')
            return render(request, self.template_name, {'tipo': request.POST.get('tipo', 'cliente')})

        # Determina tipo de conta: Empresa tem prioridade se existir
        if Empresa.objects.filter(usuario=user).exists():
            login(request, user)
            messages.success(request, 'Login de empresa efetuado.')
            return redirect('home')

        if Cliente.objects.filter(usuario=user).exists():
            login(request, user)
            messages.success(request, 'Login de cliente efetuado.')
            return redirect('home')

        # Usuário autenticado, mas sem relação Cliente/Empresa
        messages.error(request, 'Conta não vinculada como cliente ou empresa.')
        return render(request, self.template_name, {'tipo': request.POST.get('tipo', 'cliente')})
        


class perfilCliente(ListView):
    model = Cliente
    template_name = 'precocerto/cliente/perfilCliente.html'
    context_object_name = 'clientes'

#            return redirect('home')
#        return render(request, self.template_name, {'form': form})
#    
#
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


# removed separate logarEmpresa and logarCliente in favor of unified `login` class above


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
            return redirect(reverse('logar') + '?tipo=cliente')
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
            return redirect(reverse('logar') + '?tipo=cliente')
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
            return redirect(reverse('logar') + '?tipo=cliente')
        request.session['carrinho'] = {}
        return render(request, 'precocerto/cliente/carrinho.html', {'carrinho': {}, 'total': 0, 'mensagem': 'Compra confirmada com sucesso!'})

