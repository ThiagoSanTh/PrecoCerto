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
from inicialPrecoCerto.views import (#Interface
                                        paginaInicial, 
                                    #Cliente    
                                        criarCliente, logarCliente, perfilCliente,
                                    #Empresa    
                                        criarEmpresa, logarEmpresa, perfilEmpresa, 
                                    #Produto    
                                        criarProduto, detalheProduto
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
    path('perfil-empresas/', perfilEmpresa.as_view(), name='perfil_empresas'),
    
    
    #produto
    path('criar-produto/', criarProduto.as_view(), name='criar_produto'),
    path('detalhe-produto/<int:pk>/', detalheProduto.as_view(), name='detalhe_produto'),
]

if settings.DEBUG:
    urlpatterns += static(settings.MEDIA_URL, document_root=settings.MEDIA_ROOT)