using System;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

// ======================================================
// FUNCIONES
// ======================================================

static void Mostrar(string texto)
{
    Console.WriteLine(texto);
}

static void ResolverConstante(string funcion)
{
    const string exponencial = "e^(-st)";
    const string integral = "-e^(-st)/s";

    Mostrar($"\nFunción ingresada: f(t) = {funcion}");
    Mostrar("Sustituyendo en la definición de Laplace...");
    Mostrar($"∫[0,∞] {exponencial} · ({funcion}) dt");

    string multiplicacion = (funcion == "1") ? exponencial : $"{funcion}{exponencial}";

    Mostrar("Multiplicando...");
    Mostrar($"∫[0,∞] {multiplicacion} dt");

    if (funcion != "1")
    {
        Mostrar("Sacando la constante fuera de la integral...");
        Mostrar($"{funcion} ∫[0,∞] {exponencial} dt");
    }

    Mostrar("Integrando...");
    string resultadoIntegral = (funcion == "1") ? integral : $"{funcion}({integral})";
    Mostrar(resultadoIntegral);

    Mostrar("Reescribiendo la integral impropia mediante un límite...");
    string limite = $"lim b→∞ : [{resultadoIntegral}][0,b]";
    Mostrar(limite);

    Mostrar("Evaluando en los límites...");
    string superior = resultadoIntegral.Replace("t", "b");
    string inferior = resultadoIntegral.Replace("t", "0");
    Mostrar($"lim b→∞ : [{superior}] - [{inferior}]");

    Mostrar("Aplicando el límite...");
    string limiteSuperior = superior.Replace("e^(-sb)", "0").Replace("e^(-s*b)", "0");
    string limiteInferior = inferior.Replace("e^(-s0)", "1").Replace("e^(-s*0)", "1");
    Mostrar($"[{limiteSuperior}] - [{limiteInferior}]");

    Mostrar("Simplificando...");
    string resultado = (funcion == "1") ? "1/s" : $"{funcion}/s";
    Mostrar(resultado);

    Mostrar("RESULTADO FINAL");
    Mostrar($"L{{{funcion}}} = {resultado}");
}

static void ResolverPorPartesT()
{
    Mostrar("\nFunción ingresada: f(t) = t");
    Mostrar("Sustituyendo en la definición de Laplace:");
    Mostrar("  L{t} = ∫[0,∞] t · e^(-st) dt");

    Mostrar("");
    Mostrar("Aplicando Integración por Partes: ∫ u dv = u·v - ∫ v du");
    Mostrar("  u = t  -->  du = dt");
    Mostrar("  dv = e^(-st) dt  -->  v = -e^(-st) / s");

    Mostrar("");
    Mostrar("Sustituyendo en la fórmula de partes:");
    Mostrar("  = [ -t · e^(-st) / s ] [0, ∞) - ∫[0,∞] (-e^(-st) / s) dt");

    Mostrar("");
    Mostrar("Simplificando la segunda integral:");
    Mostrar("  = [ -t · e^(-st) / s ] [0, ∞) + (1/s) ∫[0,∞] e^(-st) dt");

    Mostrar("");
    Mostrar("Evaluando el término [ -t · e^(-st) / s ] de 0 a ∞:");
    Mostrar("  En ∞ tiende a 0, y en 0 se anula por t. Resultado = 0");

    Mostrar("");
    Mostrar("Integrando la parte restante y evaluando de 0 a ∞:");
    Mostrar("  = 0 + (1/s) · [ -e^(-st) / s ] [0, ∞)");
    Mostrar("  = (1/s) · (0 - (-1/s))");

    Mostrar("");
    Mostrar("RESULTADO FINAL:");
    Mostrar("  L{t} = 1 / s^2");
}

static void ResolverPorPartesT2()
{
    Mostrar("\nFunción ingresada: f(t) = t^2");
    Mostrar("Sustituyendo en la definición de Laplace:");
    Mostrar("  L{t^2} = ∫[0,∞] t^2 · e^(-st) dt");

    Mostrar("");
    Mostrar("Aplicando Integración por Partes (Primera vez):");
    Mostrar("  u = t^2  -->  du = 2t dt");
    Mostrar("  dv = e^(-st) dt  -->  v = -e^(-st) / s");

    Mostrar("");
    Mostrar("Sustituyendo en la fórmula de partes:");
    Mostrar("  = [ -t^2 · e^(-st) / s ] [0, ∞) + (2/s) ∫[0,∞] t · e^(-st) dt");

    Mostrar("");
    Mostrar("Aplicando Integración por Partes (Segunda vez):");
    Mostrar("  El término evaluado en los límites se anula (da 0).");
    Mostrar("  La integral resultante ∫ t · e^(-st) dt da como resultado 1 / s^2");

    Mostrar("");
    Mostrar("Multiplicando por el factor (2/s):");
    Mostrar("  = (2/s) · (1 / s^2)");

    Mostrar("");
    Mostrar("RESULTADO FINAL:");
    Mostrar("  L{t^2} = 2 / s^3");
}

// ======================================================
// PROGRAMA PRINCIPAL
// ======================================================

Mostrar("========================================");
Mostrar("       TRANSFORMACIONES DE LAPLACE");
Mostrar("========================================");
Mostrar("Fórmula: L{f(t)} = ∫[0,∞] e^(-st)f(t)dt");
Console.WriteLine();

string funcion = "";

do
{
    Console.Write("Ingrese la función f(t) (ej. 5, t, t^2): ");
    string entrada = Console.ReadLine();

    if (entrada != null)
    {
        funcion = entrada.Replace("$", "")
                         .Replace("f(t)", "")
                         .Replace("=", "")
                         .Trim();
    }
}
while (string.IsNullOrWhiteSpace(funcion));

// ------------------------------------------------------
// Validación y Selección de Método
// ------------------------------------------------------

if (funcion.Equals("t", StringComparison.OrdinalIgnoreCase))
{
    ResolverPorPartesT();
}
else if (funcion.Equals("t^2", StringComparison.OrdinalIgnoreCase))
{
    ResolverPorPartesT2();
}
else if (double.TryParse(funcion, out double numero))
{
    if (numero == Math.Truncate(numero))
    {
        ResolverConstante(numero.ToString());
    }
    else
    {
        Mostrar("\nPor ahora el programa trabaja con constantes enteras, t o t^2.");
    }
}
else
{
    ResolverConstante(funcion);
}

Console.WriteLine();
Mostrar("Programa terminado.");