# IA embutida do Robo

O Robo conversa com uma IA que roda **dentro do próprio PC**, sem instalar outro app, sem chave e sem enviar suas mensagens a ninguém.

## O que acontece na primeira conversa

1. O Tito baixa o motor [llama.cpp](https://github.com/ggml-org/llama.cpp) (`llama-server`, versão CPU x64 fixada em `Motor.cs`): ~25 MB.
2. Baixa o modelo escolhido em Settings → Robo (formato GGUF, Hugging Face):
   - **Qwen2.5 1.5B** (padrão, leve): ~1,1 GB, precisa de ~2,5 GB de RAM livre.
   - **Qwen2.5 3B** (melhor): ~2,1 GB, precisa de ~4 GB de RAM livre.
3. Tudo vai para `Dados\motor\` (motor e modelos). Nada é commitado no repositório.
4. O servidor escuta só em `127.0.0.1`, em porta aleatória, e é encerrado quando o Tito fecha.

Os downloads são os únicos acessos à internet. Têm barra de progresso, **retomam de onde pararam** (arquivo `.part`) e só ganham o nome final depois de conferir o **SHA-256** fixado no código. Hash errado = arquivo descartado.

Sem requisitos extras além do Windows + .NET Framework 4.8 já exigidos pelo Tito. Se faltar RAM livre ou espaço em disco, o Robo avisa com a quantidade necessária antes de baixar.

Para trocar de modelo: Settings → Robo. Para liberar espaço: apague `Dados\motor\modelos\`. O botão **Preparar IA agora** baixa e carrega tudo sem precisar conversar.

O Ollama continua funcionando como alternativa: se estiver aberto, os modelos dele aparecem na mesma lista.

## Smart App Control / SmartScreen / antivírus

Verificado no CI (Windows Server 2022): **nenhum binário do llama.cpp é assinado** (`Get-AuthenticodeSignature` = `NotSigned` em `llama-server.exe` e em todas as DLLs). Com Smart App Control (SAC) em modo ativo, o Windows pode bloquear programas sem assinatura e sem reputação; o mesmo vale para SmartScreen e alguns antivírus.

O que **não** foi possível verificar aqui: o comportamento real do SAC (não há como ligá-lo no CI nem em um Linux). O que o código faz:

- Se o Windows impedir a execução (`Process.Start` falha) ou o motor fechar logo ao iniciar, o Robo mostra a mensagem explicando o provável bloqueio e aponta este documento; o erro/saída do motor é incluído (até 400 caracteres).
- Os downloads são feitos pelo próprio Tito, então os arquivos não recebem a marca "baixado da internet" que um navegador adiciona.
- Nada pede para desativar o SAC. Se bloquear: use o Ollama (instalador assinado) como alternativa, ou, se o motor fechou por memória, escolha o modelo menor.
- Pior caso conhecido: o SAC não tem exceção por pasta nem por hash. Se ele bloquear o `llama-server.exe`, a opção embutida não funcionará nesse PC enquanto o motor não for assinado.

## Para atualizar o motor ou os modelos

Edite as constantes em `Motor.cs` (tag, URL, tamanho e SHA-256 juntos). O job `motor` do CI baixa tudo de verdade, confere os hashes, inicia o servidor e faz uma pergunta; falha se algo não bater.
