# **EcoPark VR**

## Nome do Aluno
Valter de Oliveira Lobo 

## Descrição do Projeto
**EcoPark VR** - Passeio Interativo com Reciclagem no Metaverso.

## Contexto no Metaverso
O EcoPark VR representa um parque urbano imersivo no Metaverso onde o usuário pode caminhar livremente e interagir com resíduos espalhados pelo ambiente, aprendendo sobre separação correta de lixo e sustentabilidade ambiental.

## Objetivo
Demonstrar aplicação de fundamentos XR e criação de interação funcional em ambiente VR.

## Interação Implementada


###  🔄 FLUXO DO JOGO

```
1. Jogador vê lixo no chão (5 itens espalhados)
        ↓
2. Mira no lixo → Fica amarelo (highlight)
        ↓
3. Pressiona Trigger / Clica → Lixo é coletado
   (Painel "Segurando" aparece com dica de qual lixeira)
        ↓
4a. Aproxima da lixeira CORRETA
    → Lixo some + Som de acerto + +10 pontos + Mensagem verde ✅
        ↓
4b. Aproxima da lixeira ERRADA
    → Lixo volta ao lugar + Som de erro + Mensagem vermelha ❌
        ↓
5. Após reciclar todos os 5 itens → Tela de VITÓRIA! 🏆
   (Bonus +50 se não errou nenhum)
```

---

### 🗑️ ITENS DE LIXO NA CENA

| Nome             | Tipo     | Lixeira Correta | Cor da Lixeira |
| ---------------- | -------- | --------------- | -------------- |
| PlasticBottle    | Plástico | Vermelha        | 🔴              |
| PlasticBottle_02 | Plástico | Vermelha        | 🔴              |
| Newspaper        | Papel    | Azul            | 🔵              |
| PaperBox         | Papel    | Azul            | 🔵              |
| GlassBottle      | Vidro    | Verde           | 🟢              |



## Processo de Criação
O projeto foi desenvolvido utilizando Unity LTS com Meta XR SDK, configurado para Android (Meta Quest).

## Dificuldades Encontradas
- Configuração do XR Plugin
- Ajustes de build para Android
- Simulação de interação no Editor
- Versão do unity - compatibilidade

## Tecnologias Utilizadas
- Unity
- Meta XR SDK
- C#
- GitHub

## Como Executar
1. Abrir projeto na Unity
2. Verificar XR Plugin ativo
3. Build para Android
4. Executar no Meta Quest