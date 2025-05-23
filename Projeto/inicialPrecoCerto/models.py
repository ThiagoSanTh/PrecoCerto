from django.db import models


# Create your models here.

class Produto(models.Model):
    nome = models.CharField(max_length=100)
    descricao = models.TextField()
    preco = models.DecimalField(max_digits=10, decimal_places=2)
    imagem = models.URLField(blank=True)

    def __str__(self):
        return self.nome
    
class Cliente(models.Model):
    nome = models.CharField(max_length=100)
    email = models.EmailField()
    telefone = models.CharField(max_length=11)

    def __str__(self):
        return self.nome

class Empresa(models.Model):
    nome = models.CharField(max_length=100)
    cnpj = models.CharField(max_length=14)
    endereco = models.TextField()

    def __str__(self):
        return self.nome