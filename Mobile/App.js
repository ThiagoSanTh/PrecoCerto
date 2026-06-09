import Routes from './src/navigation';
import { AuthProvider } from './src/context/AuthContext';
import { ThemeProvider } from './src/context/ThemeContext';
import { CarrinhoProvider } from './src/context/CarrinhoContext';

export default function App() {
  return (
    <ThemeProvider>
      <AuthProvider>
        <CarrinhoProvider>
          <Routes />
        </CarrinhoProvider>
      </AuthProvider>
    </ThemeProvider>
  );
}
