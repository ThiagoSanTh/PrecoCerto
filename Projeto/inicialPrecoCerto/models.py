from django.db import models
from django.contrib.auth.models import User

# Create your models here.

#-------------CLIENTE-------------------
class Cliente(models.Model):
    nome = models.CharField(max_length=100)
    usuario = models.OneToOneField(User, on_delete=models.CASCADE)
    email = models.EmailField()
    telefone = models.CharField(max_length=11)


    def __str__(self):
        return self.usuario.get_full_name() or self.usuario.username

#class Carrinho(models.Model):


#-------------EMPRESA-------------------
class Empresa(models.Model):
    nome = models.CharField(max_length=100)
    usuario = models.OneToOneField(User, on_delete=models.CASCADE)
    cnpj = models.CharField(max_length=14)
    endereco = models.TextField()

    def __str__(self):
        return self.usuario.get_full_name() or self.usuario.username
    
#-------------PRODUTO-------------------
class Produto(models.Model):
    nome = models.CharField(max_length=100)
    descricao = models.TextField()
    preco = models.DecimalField(max_digits=10, decimal_places=2)
    imagem = models.ImageField(blank=True)

    def __str__(self):
        return self.nome
