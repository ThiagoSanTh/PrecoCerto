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
    empresa = models.ForeignKey(Empresa, on_delete=models.CASCADE, related_name='produtos', null=True, blank=True)

    def __str__(self):
        return f"{self.nome} ({self.empresa.usuario.username if self.empresa else 'Sem empresa'})"


# Modelo canônico de produto (catálogo) - usado para agrupar instâncias semelhantes
class ProductMaster(models.Model):
    nome = models.CharField(max_length=200)
    descricao = models.TextField(blank=True)
    created_at = models.DateTimeField(auto_now_add=True)

    def __str__(self):
        return self.nome


# Oferta/Price listing: liga um ProductMaster a uma Empresa com um preço
class Offer(models.Model):
    product = models.ForeignKey(ProductMaster, on_delete=models.CASCADE, related_name='offers')
    empresa = models.ForeignKey(Empresa, on_delete=models.CASCADE, related_name='offers')
    price = models.DecimalField(max_digits=10, decimal_places=2)
    active = models.BooleanField(default=True)
    created_at = models.DateTimeField(auto_now_add=True)
    updated_at = models.DateTimeField(auto_now=True)

    class Meta:
        ordering = ['price', 'created_at']

    def __str__(self):
        return f"{self.product.nome} @ {self.empresa.nome} : {self.price}"


# Favoritos: cliente guarda um ProductMaster como favorito
class Favorite(models.Model):
    cliente = models.ForeignKey(Cliente, on_delete=models.CASCADE, related_name='favoritos')
    product = models.ForeignKey(ProductMaster, on_delete=models.CASCADE, related_name='favorited_by')
    created_at = models.DateTimeField(auto_now_add=True)

    class Meta:
        unique_together = ('cliente', 'product')

    def __str__(self):
        return f"{self.cliente.usuario.username} favoritou {self.product.nome}"


# Avaliações de empresas (lojas)
class Avaliacao(models.Model):
    cliente = models.ForeignKey(Cliente, on_delete=models.CASCADE, related_name='avaliacoes')
    empresa = models.ForeignKey(Empresa, on_delete=models.CASCADE, related_name='avaliacoes')
    nota = models.PositiveSmallIntegerField()
    comentario = models.TextField(blank=True, null=True)
    created_at = models.DateTimeField(auto_now_add=True)

    def __str__(self):
        return f"{self.nota} - {self.empresa.nome} by {self.cliente.usuario.username}"
