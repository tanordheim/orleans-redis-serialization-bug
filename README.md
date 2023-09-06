This bug showcases an issue when using Redis persistence in Orleans 7.2.1, where the serialized data in Redis cannot be deserialized when the grain gets reactivated.

To test the bug:

1. Run the app with Redis persistence and JSON serialization: `dotnet run --project silo/Silo.csproj -- --redis --json`. This should work OK, and the read value after grain restart should be the same as before restart.
2. Run the app with DynamoDB persistence and Orleans serialization: `dotnet run --project silo/Silo.csproj -- --dynamodb`. This should also work OK, with the read value after grain restart matching the value before restart.
3. Run the app with Redis persistence and Orleans serialization: `dotnet run --project silo/Silo.csproj -- --redis`. This will fail deserializing the persisted state when the grain reactivates.
