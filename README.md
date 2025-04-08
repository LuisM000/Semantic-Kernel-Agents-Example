# SK Agents Samples

Este proyecto contiene ejemplos de uso de agentes con Microsoft Semantic Kernel. Los agentes están diseñados para interactuar con entradas de usuario, realizar tareas específicas y colaborar entre ellos para lograr objetivos definidos.

## Estructura del Proyecto

- **`ApprovalTerminationStrategy.cs`**: Implementa una estrategia de terminación basada en la aprobación de todos los agentes.
- **`BlogInfoPlugin.cs`**: Plugin para interactuar con un blog, incluyendo la recuperación de entradas recientes.
- **`ChatHistoryFilterReducer.cs`**: Filtra el historial de chat según condiciones específicas.
- **`Program.cs`**: Punto de entrada principal del proyecto. Define y configura los agentes, y gestiona la interacción entre ellos.

## Requisitos

- **.NET 8.0 o superior**
- **Azure OpenAI**: Se requiere una cuenta y clave de API para interactuar con los modelos de OpenAI.

## Configuración

1. Configura las variables de Azure OpenAI en el archivo `Program.cs`:
   - Endpoint de Azure OpenAI
   - Clave de API de Azure OpenAI

2. Restaura las dependencias:
   ```bash
   dotnet restore
   ```

3. Compila el proyecto:
   ```bash
   dotnet build
   ```

## Ejecución

Para ejecutar el proyecto, utiliza el siguiente comando:
```bash
   dotnet run --project ./SK_Agents_Example
```