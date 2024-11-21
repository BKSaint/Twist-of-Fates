using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Collections.Specialized;
using System.Net.Security;
using System.Numerics;
using System.Net.Http.Headers;
using System.ComponentModel.Design;
using System.Xml.Serialization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using static UnityEditor.Experimental.GraphView.GraphView;
using TMPro;
using System.Timers;
using Unity.VisualScripting;
using System.ComponentModel;
using UnityEngine.UIElements;

/* totalCardValue: (initialized in Card class)
To compare the value of each card I combined the
value of the most important value (1-10, Jack(11) ..etc) 
and the suits that take precedence, which is Diamonds is 4, Hearts is 3, etc.
Its ordered in this way -> (value)(suit); 6 of diamonds is "64" or (6)(4)

For example: 
Together that makes the lowest possible TCV (totalcardValue): 21 or 2 of clubs because its value is 2 and its suit clubs
is the lowest of the 4, making it one

The highest possible value is 144 or Ace of Diamonds, since an Ace is 14 and its the highest out of the 4

*/


class GameController : MonoBehaviour
{
    public static UnityEngine.Vector3 deckPosition = new UnityEngine.Vector3(0,2,0);
    public static GameController Instance { get; private set; }
    public Transform DeckParent;
    public Transform GamesTable;
    public Transform Selection1;
    public Transform Selection2;
    public Light hoverLight;
    public TMP_Text Text1;
    public TMP_Text Text2;
    public TMP_Text Player1Score;
    public TMP_Text Player2Score;
    private Deck deck;
    private List<Player> players;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("More than one GameController in the scene!");
            Destroy(gameObject);
        }
    }
    void Start()
    {

        Player player1 = new Player(false, 1, Selection1); // User
        Player player2 = new Player(true, 2, Selection1); // CPU
        players = new List<Player> { player1, player2 };
        
        List<Transform> Selections = new List<Transform> { Selection1, Selection2 };

        deck = new Deck(1, DeckParent, GamesTable, true);

        CardSelect.OnCardClicked += PickCard;
        StartCoroutine(GameLoop());
    }
    IEnumerator GameLoop()
    {
        
        int deal = 0;
        while ((deck.GetBackendDeck().Count - deal) >= 3 )
        {
            
            foreach (Player player in players)
            {
                Print(Player1Score, players[0].points.ToString(), "Player 1: ");
                Print(Player2Score, players[1].points.ToString(), "Player 2: ");

                Print(Text1, $"{player.name}'s turn.");
                yield return new WaitForSeconds(3);
                CardSelection cards = new CardSelection(deck, player, deal);
                yield return StartCoroutine(cards.PromptPick(Text1, Text2)); // Wait for user to pick a card
                yield return new WaitForSeconds(1);
                deal+=3;
            }
            deal+=3;
        }
            
        Print(Text1, "You ran out of cards!");
        yield return new WaitForSeconds(2);

        List<int> points = new List<int>();
        players = players.OrderByDescending(player => player.points).ToList(); // Orders players from least to greatest based on points
        Print(Text1, $"{players[0].name} won with {players[0].points} points!"); // players[0] is the winner

        foreach (Player player in players) { player.Reset(); } //Resets points after game is finished    
    }
    
    private void PickCard(int cardIndex)
    {
        if (deck.GetBackendDeck()[cardIndex].flip == true)
        {
            FlipCard(cardIndex);
            deck.GetBackendDeck()[cardIndex].chosen = true;
        }
    }

    public void FlipCard(int cardIndex)
    {
        GameObject cardPicked = deck.GetVisualDeck()[cardIndex];
        float px = cardPicked.transform.localPosition.x;
        float py = cardPicked.transform.localPosition.y;
        float pz = cardPicked.transform.localPosition.z;

        float rx = cardPicked.transform.localRotation.x;

        if (rx < 0)
        {
            cardPicked.transform.localRotation = UnityEngine.Quaternion.Euler(90, 0, 0);
            cardPicked.transform.localPosition = new UnityEngine.Vector3(px, py + .5f, pz);
        }
        else
        {
            cardPicked.transform.localRotation = UnityEngine.Quaternion.Euler(-90, 0, 0);
            cardPicked.transform.localPosition = new UnityEngine.Vector3(px, py + .5f, pz);
        }
    }
    void Visibility(TMP_Text obj, bool visibility)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = visibility;
        }
    }
    void Visibility(GameObject obj, bool visibility)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = visibility;
        }
    }
    void Visibility(Light obj, bool visibility)
    {
        obj.enabled = visibility;
    }

    public void Print(TMP_Text text, string message, string startingMessage = "")
    {
        StartCoroutine(PrintLoop(text, message, startingMessage));
    }
    public IEnumerator PrintLoop(TMP_Text text, string message, string startingMessage = "")
    {
        text.text = startingMessage;
        foreach (char letter in message)
        {
            text.text += letter;
            yield return new WaitForSeconds(.04f);
        }
    }
    public void moveLight(int pos)
    {
        switch (pos) 
        {
            case 0:
                hoverLight.transform.localPosition = new UnityEngine.Vector3(3.79f, 2.58f, -3.57f);
                break;
            case 1:
                hoverLight.transform.localPosition = new UnityEngine.Vector3(3.295f, 2.546f, -3.581f);
                break;
            case 2:
                hoverLight.transform.localPosition = new UnityEngine.Vector3(2.796f, 2.346f, -3.635f);
                break;
        }
    }

    public Light getCardLight()
    {
        return hoverLight;
    }

    class Player
    {
        public int points;
        public string name;
        public int id;
        public bool cpu;
        public Transform Selection;

        public Player(bool cpu, int id, Transform Selection)
        {
            this.name = "Player" + id;
            this.id = id;
            this.cpu = cpu;
            this.Selection = Selection;
        }
        public void Reset() { points = 0; }
    }
    class Card
    {
        public int totalCardValue; // See above for definition
        private int index;
        public int value;
        public int suitLevel;
        public string suit;
        public string name;
        public string rank;
        public bool flip = false;
        public bool chosen = false;

        public Card()
        {
            suit = string.Empty;
            value = 0;
            
        }
        public Card(string card)
        {
            assignValues(card);
        }

        public void assignValues(string card)
        {
            var cardValues = card.Split(new string[] { " of " }, StringSplitOptions.None); // Splits  these into seperate variables
            string Value = cardValues[0].Trim();
            if (Value == "Jack") this.value = 11;

            else if (Value == "Queen") this.value = 12;

            else if (Value == "King") this.value = 13;

            else if (Value == "Ace") this.value = 14;

            else this.value = int.Parse(Value);

            this.rank = Value;

            this.suit = cardValues[1].Trim();

            this.suitLevel = Array.IndexOf(Deck.suits, this.suit) + 1;

            this.totalCardValue = value * 10 + suitLevel;

            this.name = $"{card}s";
        }

        public int getIndex()
        {
            return index;
        }
        public void setIndex(int index)
        {
            this.index = index;
        }

    }

    class Deck
    {
        private List<GameObject> visualDeck = new List<GameObject>();
        private List<Card> backendDeck = new List<Card>();
        public static string[] suits = { "Club", "Spade", "Heart", "Diamond" };
        public static string[] values = { "Ace", "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King" };
        public Transform DeckParent;
        private Transform GamesTable;
        public TMP_Text messageText;

        public Deck(int copies, Transform DeckParent, Transform GamesTable, bool shuffle)
        {
            this.GamesTable = GamesTable;
            this.DeckParent = DeckParent;
            for (int i = 0; i < copies; i++)
            {
                foreach (string value in values)
                {
                    foreach (string suit in suits) // Fills the deck using the arrays above
                    {
                        Card card1 = new Card($"{value} of {suit}");
                        
                        backendDeck.Add(card1);
                    }
                }
            } 
            if (shuffle) Shuffle(3);
            for (int i = 0; i < backendDeck.Count; i++) {
                backendDeck[i].setIndex(i);
            }
            Instance.StartCoroutine(SpawnCard(DeckParent));
        }

        public IEnumerator SpawnCard(Transform DeckParent)
        {
            List<Card> localDeck = new List<Card>(backendDeck);
            foreach (Card card in localDeck)
                {
                    string cardName = $"Card_{card.suit}{card.rank}";
                    GameObject cardPrefab = Resources.Load<GameObject>($"Prefabs/Individual Pieces/Cards/{card.suit}s/{cardName}");
                    if (cardPrefab != null)
                    {
                        
                  
                        GameObject visualCard = Instantiate(cardPrefab, deckPosition, UnityEngine.Quaternion.Euler(90, 0, 0), DeckParent);
                        visualCard.transform.localScale = new UnityEngine.Vector3(2, 2, 1);
                        CardSelect cardSelect = visualCard.AddComponent<CardSelect>();
                        Rigidbody rb = visualCard.GetComponent<Rigidbody>();

                        if (rb != null)
                        {
                            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                        }

                        visualCard.name = cardName;
                        visualCard.transform.localPosition = deckPosition; 

                        BoxCollider boxCollider = visualCard.GetComponent<BoxCollider>();
                        if (boxCollider != null)
                        {
                            UnityEngine.Vector3 newSize = boxCollider.size;
                            newSize.z = 0.002549444f;
                            boxCollider.size = newSize;
                        }
                        else
                        {
                            messageText.text = $"{visualCard.name} is missing a box collider";
                        }
                        cardSelect.cardIndex = visualDeck.Count;
                        visualDeck.Add(visualCard);
                        
                    yield return new WaitForSeconds(0.1f);
                    }
                    else
                    {
                        Debug.LogError($"Card prefab {cardName} not found!"); 
                    }
                }
        }
    public List<Card> GetBackendDeck() => backendDeck;
    public List<GameObject> GetVisualDeck() => visualDeck;

        public void Shuffle(int amount)
        {
            System.Random random = new System.Random();
            for (int count = 0; count < amount; count++)
            {
                for (int i = backendDeck.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    Card temp = backendDeck[i];
                    backendDeck[i] = backendDeck[j];
                    backendDeck[j] = temp;
                }
            }
        }
    }
    

    class CardSelection
    {
        public bool cardisChosen = false;
        private Card chosenCard;
        private List<Card> selectionOfCards;
        private List<Card> backendDeck;
        private List<GameObject> visualDeck;
        private static float offset = 0.5f;
        private Player player;
        private int deal;

        public CardSelection(Deck deckObj, Player player, int deal)
        {
            this.deal = deal;
            this.backendDeck = deckObj.GetBackendDeck();
            this.visualDeck = deckObj.GetVisualDeck();
            this.player = player;
            this.selectionOfCards = new List<Card>();

            for (int i = deal; i < (deal + 3); i++)
            {
                
                int iteration = i - deal;
                GameObject gameObject = visualDeck[i];
                gameObject.transform.SetParent(player.Selection.transform);
                gameObject.transform.localPosition = UnityEngine.Vector3.zero;
                selectionOfCards.Add(backendDeck[i]);
                backendDeck[i].flip = true;


                switch (iteration) {
                    case 0:
                        gameObject.transform.localPosition = new UnityEngine.Vector3(offset, 0, 0);
                        break;
                    case 1:
                        gameObject.transform.localPosition = new UnityEngine.Vector3(0, 0, 0);
                        break;
                    case 2:
                        gameObject.transform.localPosition = new UnityEngine.Vector3(-offset, 0, 0);
                        break;
                }
                

            }
            

        }
        public IEnumerator PromptPick(TMP_Text Text1, TMP_Text Text2)
        {
            //messageText.text = $"{player.name}, pick a card: 1-3"; 
            System.Random random = new System.Random();
            int choice = player.cpu ? random.Next(0, 3) : 0;
            Card hoveredCard = null;
            cardisChosen = false;
            if (!player.cpu)
            { 
                while (!cardisChosen)
                {
                    Instance.Visibility(Instance.getCardLight(), true);
                    hoveredCard = DetectHover();    
                    if (hoveredCard != null)
                    {
                        Instance.moveLight(selectionOfCards.IndexOf(hoveredCard));
                        if (hoveredCard.chosen != false)
                        {
                            Instance.moveLight(selectionOfCards.IndexOf(hoveredCard));
                            chosenCard = hoveredCard;
                            cardisChosen = true;
                        }
                    }
                    yield return null;

                }
                foreach (Card card in selectionOfCards)
                {
                    card.flip = false;
                }
                Instance.Print(Text1, $"You choose {chosenCard.name}.");
            }
            else
            {
                Instance.Print(Text1, $"{player.name} is choosing...");
                yield return new WaitForSeconds(2);

                chosenCard = selectionOfCards[choice];
                backendDeck[choice + deal].chosen = true;
                Instance.FlipCard(choice + deal);

            }
            yield return new WaitForSeconds(2.5f);
            Instance.Print(Text1, "Revealing Cards..");
            yield return new WaitForSeconds(2);

            foreach (Card card in selectionOfCards)
            {
                if (card.chosen == false) Instance.FlipCard(card.getIndex());
            }
            chosenCard.chosen = false;
            yield return new WaitForSeconds(1);
            
            int value = selectionOfCards.OrderByDescending(c => c.totalCardValue).ToList().IndexOf(chosenCard);
            switch (value)
            {
                case 0:
                    player.points += 10;
                    Instance.Print(Text1, $"{player.name} pulled the highest!");
                    yield return new WaitForSeconds(1.5f);
                    Instance.Print(Text2, "+10 points");
                    break;
                case 1:
                    player.points += 3;
                    Instance.Print(Text1, $"{player.name} pulled the middle!");
                    yield return new WaitForSeconds(1.5f);
                    Instance.Print(Text2, "+3 points");
                    break;
                case 2:
                    player.points -= 3;
                    Instance.Print(Text1, $"{player.name} pulled the lowest..");
                    yield return new WaitForSeconds(1.5f);
                    Instance.Print(Text2, "-3 points");
                    break;
            }
            yield return new WaitForSeconds(1.5f);

            Instance.Visibility(Text2, false);
            Instance.Visibility(Instance.getCardLight(), false);
            Instance.Print(Text1, $"{player.name} now has {player.points} points!");
            yield return new WaitForSeconds(5);

            for (int i = deal; i < (deal+3); i++)
            {
                visualDeck[i].transform.localPosition = new UnityEngine.Vector3(20, 20, 20);
                cardisChosen = false;
            }
        }
        private Card DetectHover()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                for (int i = 0; i < selectionOfCards.Count; i++)
                {
                    
                    if (hit.collider.gameObject == visualDeck[i+deal])
                    {
                        return selectionOfCards[i];
                    }
                }
            }

            return null; 
        }
    }
}

    