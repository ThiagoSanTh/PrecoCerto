from django import forms
from django.contrib.auth.models import User
from .models import Cliente, Empresa


class RegistroForm(forms.Form):
    """Formulário unificado de registro.

    Campos:
    - tipo: 'cliente' ou 'empresa'
    - para cliente: nome, usuario, email, senha
    - para empresa: usuario, senha, cnpj, endereco

    Validação condicional: depende de `tipo` quais campos são obrigatórios.
    Ao salvar, cria o User e a instância de Cliente ou Empresa.
    """
    TIPO_CHOICES = (('cliente', 'Cliente'), ('empresa', 'Empresa'))

    tipo = forms.ChoiceField(choices=TIPO_CHOICES, widget=forms.RadioSelect, initial='cliente', label='Registrar como')

    # Campos compartilhados/similares
    usuario = forms.CharField(max_length=150, label='Nome de usuário')
    senha = forms.CharField(widget=forms.PasswordInput, label='Senha')

    # Campos específicos de cliente
    nome = forms.CharField(max_length=100, label='Nome completo', required=False)
    email = forms.EmailField(label='E-mail', required=False)

    # Campos específicos de empresa
    cnpj = forms.CharField(max_length=14, label='CNPJ', required=False,
                           widget=forms.TextInput(attrs={'placeholder': 'Apenas números'}))
    endereco = forms.CharField(label='Endereço', required=False,
                               widget=forms.TextInput(attrs={'placeholder': 'Endereço da empresa'}))

    def clean_usuario(self):
        usuario = self.cleaned_data.get('usuario')
        if User.objects.filter(username=usuario).exists():
            raise forms.ValidationError('Nome de usuário já existe.')
        return usuario

    def clean(self):
        cleaned = super().clean()
        tipo = cleaned.get('tipo')

        if tipo == 'cliente':
            if not cleaned.get('nome'):
                self.add_error('nome', 'Nome é obrigatório para cliente.')
            if not cleaned.get('email'):
                self.add_error('email', 'E-mail é obrigatório para cliente.')

        elif tipo == 'empresa':
            if not cleaned.get('cnpj'):
                self.add_error('cnpj', 'CNPJ é obrigatório para empresa.')
            if not cleaned.get('endereco'):
                self.add_error('endereco', 'Endereço é obrigatório para empresa.')

        return cleaned

    def save(self):
        data = self.cleaned_data
        tipo = data['tipo']
        usuario = data['usuario']
        senha = data['senha']

        # Cria o User padrão do Django
        first_name = data.get('nome') if tipo == 'cliente' else usuario
        user = User.objects.create_user(
            username=usuario,
            password=senha,
            first_name=first_name,
            email=data.get('email', '')
        )

        if tipo == 'cliente':
            cliente = Cliente.objects.create(
                nome=data.get('nome'),
                usuario=user,
                email=data.get('email', ''),
            )
            return cliente

        # tipo == 'empresa'
        empresa = Empresa.objects.create(
            nome=usuario,
            usuario=user,
            cnpj=data.get('cnpj', ''),
            endereco=data.get('endereco', '')
        )
        return empresa
