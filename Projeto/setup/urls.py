"""
URL configuration for setup project.

The `urlpatterns` list routes URLs to views. For more information please see:
    https://docs.djangoproject.com/en/4.2/topics/http/urls/
Examples:
Function views
    1. Add an import:  from my_app import views
    2. Add a URL to urlpatterns:  path('', views.home, name='home')
Class-based views
    1. Add an import:  from other_app.views import Home
    2. Add a URL to urlpatterns:  path('', Home.as_view(), name='home')
Including another URLconf
    1. Import the include() function: from django.urls import include, path
    2. Add a URL to urlpatterns:  path('blog/', include('blog.urls'))
"""
from django.contrib import admin
from django.urls import path
from inicialPrecoCerto import views
from inicialPrecoCerto.views import paginaInicial, criarCliente, logarCliente, criarEmpresa,  listarEmpresas, criarProduto, listarProdutos

urlpatterns = [

    path('admin/', admin.site.urls),
    
    #interface
    path('', paginaInicial.as_view(), name='home'),
    
    #cliente
    path('criar-cliente/', criarCliente.as_view(), name='criar_cliente'),
    path('logar-cliente/', logarCliente.as_view(), name='logar_cliente'),

    #empresa
    path('criar-empresa/', criarEmpresa.as_view(), name='criar_empresa'),
    path('listar-empresas/', listarEmpresas.as_view(), name='listar_empresas'),
    
    #produto
    path('criar-produto/', criarProduto.as_view(), name='criar_produto'),
    path('listar-produtos/', listarProdutos.as_view(), name='listar_produtos'),
]