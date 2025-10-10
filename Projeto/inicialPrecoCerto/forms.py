from django import forms
from django.contrib.auth.models import User
from .models import Cliente, Empresa


class ClienteCreationForm(forms.Form):
    nome = forms.CharField(max_length=100, label='Nome completo')
    usuario = forms.CharField(max_length=150, label='Nome de usuário')
    email = forms.EmailField(label='E-mail')
    senha = forms.CharField(widget=forms.PasswordInput, label='Senha')

    def clean_usuario(self):
        usuario = self.cleaned_data.get('usuario')
        if User.objects.filter(username=usuario).exists():
            raise forms.ValidationError('Nome de usuário já existe.')
        return usuario

    def save(self):
        nome = self.cleaned_data['nome']
        usuario = self.cleaned_data['usuario']
        email = self.cleaned_data['email']
        senha = self.cleaned_data['senha']

        user = User.objects.create_user(
            username=usuario,
            password=senha,
            email=email,
            first_name=nome
        )

        cliente = Cliente.objects.create(
            nome=nome,
            usuario=user,
            email=email,
        )
        return cliente


class EmpresaCreationForm(forms.Form):
    usuario = forms.CharField(max_length=150, label='Nome da Empresa', widget=forms.TextInput(attrs={'placeholder': 'Nome da empresa/usuário'}))
    senha = forms.CharField(widget=forms.PasswordInput(attrs={'placeholder': 'Senha segura'}), label='Senha')
    cnpj = forms.CharField(max_length=14, label='CNPJ', widget=forms.TextInput(attrs={'placeholder': 'Apenas números'}))
    endereco = forms.CharField(widget=forms.TextInput(attrs={'placeholder': 'Endereço da empresa'}), label='Endereço')

    def clean_usuario(self):
        usuario = self.cleaned_data.get('usuario')
        if User.objects.filter(username=usuario).exists():
            raise forms.ValidationError('Nome de usuário já existe.')
        return usuario

    def save(self):
        usuario = self.cleaned_data['usuario']
        senha = self.cleaned_data['senha']
        cnpj = self.cleaned_data['cnpj']
        endereco = self.cleaned_data['endereco']

        user = User.objects.create_user(
            username=usuario,
            password=senha,
            first_name=usuario
        )

        empresa = Empresa.objects.create(
            nome=usuario,
            usuario=user,
            cnpj=cnpj,
            endereco=endereco
        )
        return empresa
