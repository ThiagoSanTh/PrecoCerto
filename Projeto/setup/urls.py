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
from django.conf.urls.static import static
from django.conf import settings

from inicialPrecoCerto.views import (
    
    paginaInicial,
    
    criarCliente, logarCliente, perfilCliente,

    criarEmpresa, deletarEmpresa, logarEmpresa, perfilEmpresa, logoutEmpresa, editarEmpresa,
    
    criarProduto, detalheProduto, editarProduto, deletarProduto, 
    
    adicionarCarrinho, verCarrinho, alterarQuantidade, removerCarrinho, confirmarCompra
)

urlpatterns = [
    
    path('admin/', admin.site.urls),
        
    #interface
    path('', paginaInicial.as_view(), name='home'),
    
    #cliente
    path('criar-cliente/', criarCliente.as_view(), name='criar_cliente'),
    path('logar-cliente/', logarCliente.as_view(), name='logar_cliente'),
    path('perfil-cliente/', perfilCliente.as_view(), name='perfil_cliente'),

    #empresa
    path('criar-empresa/', criarEmpresa.as_view(), name='criar_empresa'),
    path('logar-empresa/', logarEmpresa.as_view(), name='logar_empresa'),
    path('logout/', logoutEmpresa.as_view(), name='logout'),
    path('perfil-empresas/', perfilEmpresa.as_view(), name='perfil_empresas'),
    path('deletar-empresa/<int:pk>/', deletarEmpresa.as_view(), name='deletar_empresa'),
    path('editar-empresa/<int:pk>/', editarEmpresa.as_view(), name='editar_empresa'),
    
    #produto
    path('criar-produto/', criarProduto.as_view(), name='criar_produto'),
    path('editar-produto/<int:pk>/', editarProduto.as_view(), name='editar_produto'),
    path('deletar-produto/<int:pk>/', deletarProduto.as_view(), name='deletar_produto'),
    path('detalhe-produto/<int:pk>/', detalheProduto.as_view(), name='detalhe_produto'),

    #carrinho
    path('adicionar-carrinho/<int:produto_id>/', adicionarCarrinho.as_view(), name='adicionar_carrinho'),
    path('carrinho/', verCarrinho.as_view(), name='ver_carrinho'),
    path('alterar-quantidade/<int:produto_id>/', alterarQuantidade.as_view(), name='alterar_quantidade'),
    path('remover-carrinho/<int:produto_id>/', removerCarrinho.as_view(), name='remover_carrinho'),
    path('confirmar-compra/', confirmarCompra.as_view(), name='confirmar_compra'),



]

if settings.DEBUG:
    urlpatterns += static(settings.MEDIA_URL, document_root=settings.MEDIA_ROOT)

