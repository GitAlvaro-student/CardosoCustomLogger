# 🧭 Planejamento Arquitetural – Evolução do CustomLogger

Este documento apresenta o **planejamento arquitetural completo**, detalhado e oficial para a evolução do framework de logging customizado (.NET Framework 4.7.2 e .NET 8).

Ele foi elaborado com base na análise da aplicação existente e tem como foco:
- segurança arquitetural
- evolução sustentável
- observabilidade
- performance futura
- manutenibilidade de longo prazo

---

# 🔹 PLANEJAMENTO – PASSOS JÁ IMPLEMENTADOS

---

## 🥇 Passo 1 — Abstractions (Fundação)

### 🎯 Objetivo do planejamento
Fortalecer contratos para **segurança futura**, **imutabilidade** e **compatibilidade com concorrência e async**.

### Pontos de atenção identificados
- `ILogEntry` não é totalmente imutável
- Contratos ainda não comunicam claramente evolução futura (async, batch, resiliência)

### Ações arquiteturais planejadas
- Consolidar `ILogEntry` como **Value Object**
- Garantir imutabilidade após a criação
- Definir scopes apenas no momento da construção
- Evitar qualquer mutação no pipeline

- Formalizar responsabilidades dos contratos:
  - `ILogEntry` = dado
  - `ILogBuffer` = orquestração temporária
  - `ILogSink` = fronteira externa (falha possível)

- Documentar evolução esperada:
  - Contratos síncronos permanecem válidos
  - Versões assíncronas serão **extensões**, não substituições

### 📌 Resultado esperado
Abstrações estáveis por anos, mesmo com async, batch e backpressure.

---

## 🥈 Passo 2 — Primeiro Sink (MVP)

### 🎯 Objetivo do planejamento
Manter o MVP simples, mas **formalmente correto**, servindo de base para testes e fallback.

### Pontos de atenção identificados
- Sink simples não trata falhas
- Ausência do conceito de “última linha de defesa”

### Ações arquiteturais planejadas
- Definir comportamento mínimo de falha:
  - Um sink nunca deve derrubar a aplicação
  - Falhas devem ser absorvidas localmente

- Estabelecer o conceito de **sink de fallback**:
  - Ultra-simples
  - Sem dependências
  - Nunca lança exceção

### 📌 Resultado esperado
Mesmo o sink mais simples passa a ser confiável em produção.

---

## 🥉 Passo 3 — Conexão Buffer → Sink

### 🎯 Objetivo do planejamento
Eliminar **acoplamento implícito** e **estado global**, preparando o buffer para escala.

### Pontos de atenção identificados
- Buffer global estático
- Estado compartilhado entre providers
- Dependência indireta de tipos concretos

### Ações arquiteturais planejadas
- Migrar conceitualmente de “buffer global” para “buffer por pipeline”:
  - Um buffer por provider
  - Um pipeline = buffer + sinks

- Eliminar dependência de estado estático:
  - Buffer passa a ser instância
  - Provider passa a ser o dono do buffer

- Garantir que o buffer aceite qualquer `ILogEntry`:
  - Sem verificações de tipo concreto
  - Respeito total à abstração

### 📌 Resultado esperado
Múltiplos pipelines independentes, testáveis e seguros.

---

## 🧩 Passo 4 — Provider como Orquestrador

### 🎯 Objetivo do planejamento
Evitar que o Provider evolua para um **God Object**.

### Pontos de atenção identificados
- Criação de sinks hardcoded
- Configuração pouco extensível
- Ciclo de vida incompleto (Dispose parcial)

### Ações arquiteturais planejadas
- Separar composição de responsabilidade:
  - Provider orquestra
  - Outra entidade define **como** os sinks são criados

- Planejar modelo de configuração extensível:
  - Builder ou Factory (conceitualmente)
  - Configuração fora do código do provider

- Formalizar gestão de ciclo de vida:
  - Provider é dono do buffer
  - Provider é dono dos sinks
  - Provider é responsável por flush
  - Provider é responsável por dispose

### 📌 Resultado esperado
Provider previsível, extensível e seguro para aplicações long-lived.

---

## 🟦 Passo 5 — Padronização de Formato

### 🎯 Objetivo do planejamento
Garantir que o formato JSON seja **robusto**, **versionável** e **seguro**.

### Pontos de atenção identificados
- Serialização pode falhar
- Exceções podem gerar payloads enormes
- `State` pode conter dados sensíveis ou não serializáveis

### Ações arquiteturais planejadas
- Definir política de tolerância a falhas de serialização:
  - Formatter nunca lança exceção
  - Fallback seguro (string, tipo, hash)

- Estruturar exceções:
  - Separar tipo
  - Separar mensagem
  - Separar stack trace
  - Evitar logs gigantes

- Pensar em versionamento de schema:
  - Mesmo sem implementar agora
  - Garantir compatibilidade futura

### 📌 Resultado esperado
Formato estável, observável e pronto para ingestão por ferramentas externas.

---

## 🧠 Passo 6 — Scope e Observabilidade

### 🎯 Objetivo do planejamento
Consolidar observabilidade sem impacto excessivo em performance.

### Pontos de atenção identificados
- Criação de dicionário a cada log
- Colisão silenciosa de chaves
- Custo crescente em cenários de alto volume

### Ações arquiteturais planejadas
- Definir regra oficial de colisão de escopos:
  - Último vence
  - Agregação
  - Prefixação
  - (documentar claramente)

- Planejar cache conceitual de scopes:
  - Cache invalidado quando o scope muda
  - Evitar recomposição em cada log

- Manter isolamento por `AsyncLocal`:
  - Decisão correta
  - Base sólida para tracing

### 📌 Resultado esperado
Observabilidade rica, previsível e pronta para correlação distribuída.

---

# 🚀 PLANEJAMENTO – PRÓXIMOS PASSOS

---

## 🚀 Passo 7 — Performance e Resiliência

### 🎯 Objetivo
Escalar logging **sem impactar a aplicação**.

### Eixos arquiteturais do passo
- **Batch**:
  - Reduz I/O
  - Aumenta throughput
  - Controla flush

- **Async**:
  - Evita bloqueio de threads
  - Isola latência externa

- **Backpressure**:
  - Buffer com limite
  - Política clara de overflow:
    - descartar
    - bloquear
    - priorizar

- **Resiliência**:
  - Try/catch no pipeline
  - Circuit breaker por sink
  - Fallback sempre disponível

- **Shutdown seguro**:
  - Flush final garantido
  - Sem perda silenciosa de logs

### 📌 Resultado esperado
Logging invisível para a aplicação, mesmo sob falha ou pico.

---

## 🧪 Passo 8 — Testes

### 🎯 Objetivo
Garantir **confiança evolutiva** do framework.

### Tipos de testes planejados
- **Testes de Logger**:
  - Entrada → saída esperada
  - Sem dependência de I/O real

- **Testes de Buffer**:
  - Ordem
  - Limites
  - Flush

- **Testes de Sink**:
  - Comportamento sob falha
  - Resiliência

- **Testes de Scope**:
  - Isolamento
  - Aninhamento
  - Concorrência

### Decisões arquiteturais importantes
- Todo componente deve ser testável isoladamente
- Nenhum teste depende de Console, File ou Blob reais
- Mocks como cidadãos de primeira classe

### 📌 Resultado esperado
Evolução segura, refatorações sem medo e base para NuGet público.

---

# 🏁 Conclusão

Este planejamento:
- respeita a arquitetura atual
- resolve todos os pontos de atenção levantados
- prepara o projeto para escala real
- mantém o principal diferencial: **controle total da arquitetura**

Este documento deve ser tratado como **referência oficial de arquitetura** para o CustomLogger.
