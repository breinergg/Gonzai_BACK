using System.Buffers.Binary;

namespace Gonzai_API.Services.Export;

internal static class LogoPng
{
    public const int MaxBytes = 500 * 1024;
    public const int MaxLado = 2000;

    public static void Validar(byte[] bytes)
    {
        if (bytes.Length == 0 || bytes.Length > MaxBytes)
            throw new InvalidOperationException("El logo debe ser un PNG de máximo 500 KB.");

        ReadOnlySpan<byte> firma = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        if (bytes.Length < 24 || !bytes.AsSpan(0, 8).SequenceEqual(firma))
            throw new InvalidOperationException("El archivo debe ser un PNG.");

        if (bytes[12] != (byte)'I' || bytes[13] != (byte)'H' || bytes[14] != (byte)'D' || bytes[15] != (byte)'R')
            throw new InvalidOperationException("El PNG no es válido.");

        var ancho = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4));
        var alto = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4));

        if (ancho <= 0 || alto <= 0 || ancho > MaxLado || alto > MaxLado)
            throw new InvalidOperationException(
                $"El logo no puede superar {MaxLado} px por lado. Recomendado: 600×200 (horizontal) o 400×400 (cuadrado).");
    }
}
