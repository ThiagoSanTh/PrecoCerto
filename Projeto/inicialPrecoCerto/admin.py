from django.contrib import admin
from .models import (
	Cliente, Empresa, Produto,
	ProductMaster, Offer, Favorite, Avaliacao
)


@admin.register(Cliente)
class ClienteAdmin(admin.ModelAdmin):
	list_display = ('usuario', 'nome', 'email')


@admin.register(Empresa)
class EmpresaAdmin(admin.ModelAdmin):
	list_display = ('usuario', 'nome', 'cnpj')


@admin.register(Produto)
class ProdutoAdmin(admin.ModelAdmin):
	list_display = ('nome', 'empresa', 'preco')


@admin.register(ProductMaster)
class ProductMasterAdmin(admin.ModelAdmin):
	list_display = ('nome', 'created_at')


@admin.register(Offer)
class OfferAdmin(admin.ModelAdmin):
	list_display = ('product', 'empresa', 'price', 'active', 'created_at')


@admin.register(Favorite)
class FavoriteAdmin(admin.ModelAdmin):
	list_display = ('cliente', 'product', 'created_at')


@admin.register(Avaliacao)
class AvaliacaoAdmin(admin.ModelAdmin):
	list_display = ('cliente', 'empresa', 'nota', 'created_at')
