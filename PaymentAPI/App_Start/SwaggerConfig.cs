using PaymentAPI;
using Swashbuckle.Application;
using System;
using System.IO;
using System.Reflection;
using System.Web.Http;
using WebActivatorEx;

[assembly: PreApplicationStartMethod(typeof(SwaggerConfig), "Register")]

namespace PaymentAPI
{
    public class SwaggerConfig
    {
        public static void Register()
        {
            var thisAssembly = typeof(SwaggerConfig).Assembly;

            GlobalConfiguration.Configuration
                .EnableSwagger(c =>
                {
                    c.SingleApiVersion("v1", "Payment API");

                    // Tentar incluir comentários XML, mas não falhar se não existir
                    try
                    {
                        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                        var xmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", xmlFile);

                        if (File.Exists(xmlPath))
                        {
                            c.IncludeXmlComments(xmlPath);
                        }
                        else
                        {
                            // Cria um arquivo XML vazio para evitar o erro
                            CreateEmptyXmlFile(xmlPath);
                            c.IncludeXmlComments(xmlPath);
                        }
                    }
                    catch
                    {
                        // Ignora erros relacionados ao XML
                    }

                    // Configurar para trabalhar com WebAPI
                    c.DescribeAllEnumsAsStrings();
                })
                .EnableSwaggerUi(c =>
                {
                    // Personalizações opcionais do Swagger UI
                    c.DocumentTitle("API de Pagamentos");
                });
        }

        private static void CreateEmptyXmlFile(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Cria um arquivo XML básico
            string emptyXml = @"<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>PaymentAPI</name>
    </assembly>
    <members>
    </members>
</doc>";

            File.WriteAllText(path, emptyXml);
        }
    }
}

