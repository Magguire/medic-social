var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.MedSocial2_Api>("api")
    .WithExternalHttpEndpoints();

builder.AddNpmApp("client-next", "../../apps/client-next", "dev")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(env: "PORT")
    .WithEnvironment("NEXT_PUBLIC_API_BASE_URL", api.GetEndpoint("http"));

builder.AddNpmApp("admin-next", "../../apps/admin-next", "dev")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(env: "PORT")
    .WithEnvironment("NEXT_PUBLIC_API_BASE_URL", api.GetEndpoint("http"));

builder.AddProject<Projects.admin_blazor>("admin-blazor")
    .WithExternalHttpEndpoints();

builder.AddContainer("prometheus", "prom/prometheus")
    .WithHttpEndpoint(port: 9090, targetPort: 9090)
    .WithBindMount("../../docs/observability/prometheus.yml", "/etc/prometheus/prometheus.yml", isReadOnly: true);

builder.AddContainer("grafana", "grafana/grafana")
    .WithHttpEndpoint(port: 3002, targetPort: 3000)
    .WithEnvironment("GF_SECURITY_ADMIN_USER", "admin")
    .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", "admin12345");

builder.Build().Run();
