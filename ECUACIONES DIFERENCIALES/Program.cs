// ============================================================================
// TRANSFORMADAS DE LAPLACE PASO A PASO
// ----------------------------------------------------------------------------
// Programa de consola que recibe una función f(t) = k · t^n (k entero, n entero
// entre 0 y 10) y muestra el procedimiento completo para obtener su
// transformada de Laplace:
//
//     L{f(t)} = ∫[0,∞] e^(-st) · f(t) dt
//
// Resultado general que el programa demuestra:
//     L{k · t^n} = k · n! / s^(n+1)      (válido para s > 0)
//
// Estructura del archivo:
//   1. Programa principal (flujo de ejecución)
//   2. FuncionPotencia    -> modelo + validación de la entrada
//   3. ExplicadorLaplace  -> genera el texto del procedimiento
// ============================================================================

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

// ======================================================
// PROGRAMA PRINCIPAL
// ======================================================

// Necesario para que la consola muestre símbolos como ∫, ∞ y →.
Console.OutputEncoding = Encoding.UTF8;

// El explicador escribe en la consola, pero recibe un TextWriter para poder
// probarlo más adelante escribiendo en un StringWriter.
var explicador = new ExplicadorLaplace(Console.Out);
explicador.MostrarEncabezado();

FuncionPotencia? funcion = LeerFuncion();

// LeerFuncion devuelve null solo si la entrada estándar se cerró
// (Ctrl+Z / Ctrl+D o una redirección que ya se agotó).
if (funcion is null)
{
    Console.WriteLine();
    Console.WriteLine("Entrada finalizada. Programa terminado.");
    return;
}

explicador.Resolver(funcion);

Console.WriteLine();
Console.WriteLine("Programa terminado.");

// ------------------------------------------------------
// Entrada de datos
// ------------------------------------------------------

/// <summary>
/// Pide la función al usuario hasta recibir una válida.
/// </summary>
/// <returns>
/// La función ingresada, o <c>null</c> si la entrada estándar se cerró
/// (esto evita un bucle infinito cuando ReadLine devuelve null).
/// </returns>
static FuncionPotencia? LeerFuncion()
{
    while (true)
    {
        Console.Write("Ingrese la función f(t) (ej. 5, t, 3t^2): ");
        string? entrada = Console.ReadLine();

        // ReadLine devuelve null cuando no hay más datos que leer.
        if (entrada is null)
        {
            return null;
        }

        // Si el formato es correcto, terminamos; si no, avisamos y repetimos.
        if (FuncionPotencia.TryParse(entrada, out FuncionPotencia? funcion))
        {
            return funcion;
        }

        Console.WriteLine(
            $"  Entrada no válida. Use constantes enteras o términos k·t^n (n entre 0 y {FuncionPotencia.ExponenteMaximo}).");
    }
}

// ======================================================
// MODELO
// ======================================================

/// <summary>
/// Función de la forma f(t) = k · t^n, con k entero y n entero no negativo.
/// Las constantes son el caso particular n = 0 (por ejemplo, 5 = 5·t^0).
/// Es un record inmutable: una vez creada no cambia.
/// </summary>
/// <param name="Coeficiente">El valor k (puede ser negativo o cero).</param>
/// <param name="Exponente">El valor n (de 0 a <see cref="ExponenteMaximo"/>).</param>
internal sealed record FuncionPotencia(BigInteger Coeficiente, int Exponente)
{
    /// <summary>
    /// Límite superior del exponente. Evita salidas gigantescas
    /// y factoriales innecesariamente grandes.
    /// </summary>
    public const int ExponenteMaximo = 10;

    // Formato aceptado (ya sin espacios y en minúsculas):
    //   k   -> coeficiente opcional con signo      ej. "", "5", "-3", "+2", "-"
    //   t   -> variable opcional                   ej. "t"
    //   n   -> exponente opcional si hay "t"       ej. "^2"
    // Ejemplos válidos: "5", "t", "-t", "3t", "3t^2", "-4t^10".
    // Se compila una sola vez (static readonly) porque se reutiliza.
    private static readonly Regex Patron = new(
        @"^(?<k>[+-]?\d*)(?<t>t(\^(?<n>\d+))?)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Intenta interpretar el texto del usuario como k·t^n.
    /// Sigue el patrón "Try": no lanza excepciones, devuelve false si no es válido.
    /// </summary>
    /// <param name="entrada">Texto escrito por el usuario.</param>
    /// <param name="funcion">La función resultante; no es null cuando devuelve true.</param>
    public static bool TryParse(string entrada, [NotNullWhen(true)] out FuncionPotencia? funcion)
    {
        funcion = null;

        string texto = Normalizar(entrada);
        Match coincidencia = Patron.Match(texto);

        if (!coincidencia.Success)
        {
            return false;
        }

        string textoK = coincidencia.Groups["k"].Value;
        bool contieneT = coincidencia.Groups["t"].Success;

        // El patrón también acepta "", "+" y "-" (porque k es opcional).
        // Si no hay "t", eso no sería una constante válida: exigimos un dígito.
        if (!contieneT && !textoK.Any(char.IsDigit))
        {
            return false;
        }

        // Interpretación del coeficiente:
        //   "" o "+"  -> 1   (caso "t" o "+t")
        //   "-"       -> -1  (caso "-t")
        //   otro      -> número entero (InvariantCulture evita depender del idioma del equipo)
        BigInteger coeficiente = textoK switch
        {
            "" or "+" => BigInteger.One,
            "-" => BigInteger.MinusOne,
            _ => BigInteger.Parse(textoK, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture)
        };

        // Sin "t" es una constante, es decir, exponente 0.
        int exponente = 0;

        if (contieneT)
        {
            // "t" a secas equivale a t^1.
            exponente = 1;

            // Si el usuario escribió "^n", se reemplaza el valor por defecto.
            // int.TryParse falla si el número es tan grande que desborda un int.
            Group grupoN = coincidencia.Groups["n"];
            if (grupoN.Success &&
                !int.TryParse(grupoN.Value, NumberStyles.None, CultureInfo.InvariantCulture, out exponente))
            {
                return false;
            }
        }

        if (exponente > ExponenteMaximo)
        {
            return false;
        }

        funcion = new FuncionPotencia(coeficiente, exponente);
        return true;
    }

    /// <summary>
    /// Representación legible: "5", "t", "-t", "3t^2", etc.
    /// Omite el coeficiente cuando es 1 y usa solo "-" cuando es -1.
    /// </summary>
    public override string ToString()
    {
        if (Exponente == 0)
        {
            return Coeficiente.ToString(CultureInfo.InvariantCulture);
        }

        string prefijo =
            Coeficiente.IsOne ? "" :
            Coeficiente == BigInteger.MinusOne ? "-" :
            Coeficiente.ToString(CultureInfo.InvariantCulture);

        return prefijo + FormatoT(Exponente);
    }

    /// <summary>
    /// Devuelve la parte de la variable: "1" (n=0), "t" (n=1) o "t^n" (n&gt;1).
    /// </summary>
    public static string FormatoT(int exponente) => exponente switch
    {
        0 => "1",
        1 => "t",
        _ => $"t^{exponente}"
    };

    /// <summary>
    /// Limpia el texto para que el patrón sea sencillo:
    /// quita símbolos de formato, espacios y el prefijo "f(t)=".
    /// </summary>
    private static string Normalizar(string entrada)
    {
        string texto = entrada
            .Replace("$", "")   // por si se pega desde LaTeX
            .Replace("·", "")   // "3·t" -> "3t"
            .Replace("*", "")   // "3*t" -> "3t"
            .Replace(" ", "")
            .ToLowerInvariant(); // "T^2" -> "t^2"

        // Acepta "f(t)=..." (ya sin espacios) o solo "=...".
        if (texto.StartsWith("f(t)", StringComparison.Ordinal))
        {
            texto = texto[4..];
        }

        return texto.TrimStart('=');
    }
}

// ======================================================
// PRESENTACIÓN DEL PROCEDIMIENTO
// ======================================================

/// <summary>
/// Escribe el desarrollo matemático paso a paso.
/// No hace cálculos simbólicos: aplica la fórmula general L{k·t^n} = k·n!/s^(n+1)
/// y muestra el razonamiento (integración por partes repetida) que la justifica.
/// </summary>
internal sealed class ExplicadorLaplace
{
    // Destino del texto (consola, archivo o StringWriter en pruebas).
    private readonly TextWriter _salida;

    public ExplicadorLaplace(TextWriter salida)
    {
        _salida = salida ?? throw new ArgumentNullException(nameof(salida));
    }

    /// <summary>Imprime el título y la definición de la transformada.</summary>
    public void MostrarEncabezado()
    {
        Linea("========================================");
        Linea("       TRANSFORMACIONES DE LAPLACE");
        Linea("========================================");
        Linea("Fórmula: L{f(t)} = ∫[0,∞] e^(-st) f(t) dt");
        Linea();
    }

    /// <summary>
    /// Muestra el procedimiento completo y el resultado final para <paramref name="f"/>.
    /// </summary>
    public void Resolver(FuncionPotencia f)
    {
        Linea();
        Linea($"Función ingresada: f(t) = {f}");
        // Sin s > 0 la integral impropia diverge, por eso se aclara desde el inicio.
        Linea("Se asume s > 0 para que la integral converja.");
        Linea("Sustituyendo en la definición de Laplace:");
        Linea($"  L{{{f}}} = ∫[0,∞] ({f}) · e^(-st) dt");

        // Caso especial: f(t) = 0 -> la transformada es 0, no hay nada que integrar.
        if (f.Coeficiente.IsZero)
        {
            Linea("La función nula tiene transformada 0.");
            MostrarResultado(f, "0");
            return;
        }

        // Propiedad de linealidad: las constantes salen de la integral.
        // Si el coeficiente es 1, este paso sería redundante y se omite.
        if (!f.Coeficiente.IsOne)
        {
            Linea();
            Linea("Sacando la constante fuera de la integral:");
            Linea($"  = {f.Coeficiente} · ∫[0,∞] {Integrando(f.Exponente)} dt");
        }

        if (f.Exponente == 0)
        {
            // Constante: basta con la integral de e^(-st).
            ResolverIntegralBase();
            Linea();
            Linea($"Multiplicando por la constante {f.Coeficiente}:");
        }
        else
        {
            // Potencia de t: se necesita integración por partes n veces.
            ResolverPorPartes(f.Exponente);
            Linea();
            Linea("Combinando los factores:");
            Linea($"  n! / s^(n+1) con n = {f.Exponente}");
            if (!f.Coeficiente.IsOne)
            {
                Linea($"  y multiplicando por {f.Coeficiente}");
            }
        }

        // Resultado general: k · n! / s^(n+1).
        // Para n = 0, 0! = 1, así que se reduce a k / s.
        BigInteger numerador = f.Coeficiente * Factorial(f.Exponente);
        MostrarResultado(f, Fraccion(numerador, f.Exponente + 1));
    }

    // ------------------------------------------------------
    // Pasos intermedios
    // ------------------------------------------------------

    /// <summary>
    /// Resuelve ∫[0,∞] e^(-st) dt = 1/s.
    /// Es el caso base de toda la demostración (constantes y fin de las
    /// integraciones por partes).
    /// </summary>
    private void ResolverIntegralBase()
    {
        Linea();
        Linea("Integrando ∫[0,∞] e^(-st) dt:");
        Linea("  = lim b→∞ [ -e^(-st)/s ] de 0 a b");
        Linea("  = lim b→∞ ( -e^(-sb)/s + e^0/s )");
        Linea("  Como s > 0, e^(-sb) → 0 cuando b → ∞, y e^0 = 1:");
        Linea("  = 1/s");
    }

    /// <summary>
    /// Aplica integración por partes n veces sobre ∫ t^n e^(-st) dt.
    /// En cada vuelta se baja el exponente en 1:
    ///     ∫ t^i e^(-st) dt = (i/s) · ∫ t^(i-1) e^(-st) dt
    /// hasta llegar a la integral base de e^(-st).
    /// </summary>
    /// <param name="n">Exponente inicial (mayor o igual que 1).</param>
    private void ResolverPorPartes(int n)
    {
        for (int i = n; i >= 1; i--)
        {
            Linea();
            Linea($"Integración por partes ({n - i + 1} de {n}) sobre ∫[0,∞] {Integrando(i)} dt:");
            // Elección clásica: u se deriva (baja el grado) y dv se integra.
            Linea($"  u = {FuncionPotencia.FormatoT(i)}  -->  du = {Derivada(i)} dt");
            Linea("  dv = e^(-st) dt  -->  v = -e^(-st) / s");
            Linea($"  = [ -{FuncionPotencia.FormatoT(i)} · e^(-st) / s ] [0, ∞) + ({i}/s) ∫[0,∞] {Integrando(i - 1)} dt");
            // El término u·v evaluado en los límites siempre vale 0 cuando s > 0.
            Linea("  El término evaluado vale 0: en ∞ domina e^(-st) (s > 0) y en 0 se anula por t.");
            Linea($"  = ({i}/s) ∫[0,∞] {Integrando(i - 1)} dt");
        }

        // Tras bajar el exponente hasta 0 queda ∫ e^(-st) dt.
        ResolverIntegralBase();

        // Producto de todas las fracciones acumuladas: (n/s)·((n-1)/s)···(1/s)·(1/s).
        Linea();
        string factores = string.Join(" · ", Enumerable.Range(1, n).Reverse().Select(k => $"({k}/s)"));
        Linea($"Acumulando los factores: {factores} · (1/s)");
    }

    /// <summary>Imprime el bloque "RESULTADO FINAL".</summary>
    private void MostrarResultado(FuncionPotencia f, string resultado)
    {
        Linea();
        Linea("RESULTADO FINAL:");
        Linea($"  L{{{f}}} = {resultado}");
    }

    // ------------------------------------------------------
    // Utilidades de formato y cálculo
    // ------------------------------------------------------

    /// <summary>
    /// Texto del integrando sin el coeficiente: "e^(-st)" (n=0) o "t^n · e^(-st)".
    /// </summary>
    private static string Integrando(int exponente) =>
        exponente == 0 ? "e^(-st)" : $"{FuncionPotencia.FormatoT(exponente)} · e^(-st)";

    /// <summary>
    /// Derivada de t^n en forma de texto: 1, 2t o n·t^(n-1).
    /// Los casos 1 y 2 se escriben aparte para no mostrar "1t^0" ni "2t^1".
    /// </summary>
    private static string Derivada(int exponente) => exponente switch
    {
        1 => "1",
        2 => "2t",
        _ => $"{exponente}t^{exponente - 1}"
    };

    /// <summary>
    /// Formatea numerador / s^potencia. Si el numerador es 0 devuelve "0".
    /// Evita escribir "s^1" (se muestra solo "s").
    /// </summary>
    private static string Fraccion(BigInteger numerador, int potenciaS)
    {
        if (numerador.IsZero)
        {
            return "0";
        }

        string denominador = potenciaS == 1 ? "s" : $"s^{potenciaS}";
        return $"{numerador}/{denominador}";
    }

    /// <summary>Factorial de n usando BigInteger (0! = 1! = 1).</summary>
    private static BigInteger Factorial(int n)
    {
        BigInteger resultado = BigInteger.One;
        for (int i = 2; i <= n; i++)
        {
            resultado *= i;
        }

        return resultado;
    }

    /// <summary>Atajo para escribir una línea en el destino configurado.</summary>
    private void Linea(string texto = "") => _salida.WriteLine(texto);
}