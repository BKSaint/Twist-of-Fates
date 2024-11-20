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
    public TMP_Text messageText;
    private Deck deck;
    private List<Player> players;
    public

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
        Player player2 = new Player(true, 2, Selection2); // CPUa
        players = new List<Player> { player1, player2 };
        
        List<Transform> Selections = new List<Transform> { Selection1, Selection2 };

        deck = new Deck(1, DeckParent, GamesTable, true);

        CardSelect.OnCardClicked += PickCard;
        StartCoroutine(GameLoop());
    }
    IEnumerator GameLoop()
    {
        int deal = 0;
        while (deck.GetBackendDeck().Count >= 6)
        {
            foreach (Player player in players)
            {
                yield return new WaitForSeconds(5);
                CardSelection cards = new CardSelection(deck, player, (deal*3));
                yield return StartCoroutine(cards.PromptPick(messageText)); // Wait for user to pick a card
                yield return new WaitForSeconds(1);

                messageText.text = $"{player.name} picked a card!";
                yield return new WaitForSeconds(2);
            }
            deal++;
        }
            
        messageText.text = "You ran out of cards!";
        yield return new WaitForSeconds(2);

        List<int> points = new List<int>();
        players = players.OrderByDescending(player => player.points).ToList(); // Orders players from least to greatest based on points
        messageText.text = $"{players[0].name} won with {players[0].points} points!"; // players[0] is the winner

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
        Debug.Log($"Flipped: {deck.GetBackendDeck()[cardIndex].name}");
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
        public static string[] suits = { "Diamond", "Heart", "Spade", "Club" };
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
            Debug.Log("Creating Deck...");
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


        public CardSelection(Deck deckObj, Player player, int deal)
        {
            this.backendDeck = deckObj.GetBackendDeck();
            this.visualDeck = deckObj.GetVisualDeck();
            this.player = player;
            this.selectionOfCards = new List<Card>();

            for (int i = deal; i < (deal + 3); i++)
            {
                
                int iteration = i - deal;
                Debug.Log(iteration);
                GameObject gameObject = visualDeck[i];
                gameObject.transform.SetParent(player.Selection.transform);
                gameObject.transform.localPosition = UnityEngine.Vector3.zero;
                selectionOfCards.Add(backendDeck[i]);
                backendDeck[i].flip = true;


                switch (iteration) {
                    case 0:
                        gameObject.transform.localPosition = new UnityEngine.Vector3(offset, 0, 0);
                        Debug.Log("Created card1");
                        break;
                    case 1:
                        gameObject.transform.localPosition = new UnityEngine.Vector3(0, 0, 0);
                        Debug.Log("Created card2");
                        break;
                    case 2:
                        gameObject.transform.localPosition = new UnityEngine.Vector3(-offset, 0, 0);
                        Debug.Log("Created card3");
                        break;
                }
                

            }
            

        }
        public IEnumerator PromptPick(TMP_Text messageText)
        {
            //messageText.text = $"{player.name}, pick a card: 1-3"; 
            System.Random random = new System.Random();
            int choice = player.cpu ? random.Next(1, 4) : 0;
            if (!player.cpu)
            {
                while (!cardisChosen)
                {
                    foreach (Card card in selectionOfCards)
                        if (card.chosen != false)
                        {
                            chosenCard = card;
                            cardisChosen = true;
                        }
                    yield return null;
                    
                }
                foreach (Card card in selectionOfCards)
                {
                    card.flip = false;
                }
                Debug.Log($"You choose {chosenCard.name}");
            }

            Debug.Log("Revealing Cards..");
            yield return new WaitForSeconds(5);
            foreach (Card card in selectionOfCards)
            {
                if (card.chosen == false) Instance.FlipCard(card.getIndex());
            }
            chosenCard.chosen = false;
            EvaluatePick(chosenCard, messageText);

            yield return new WaitForSeconds(1);
        }
        private void EvaluatePick(Card chosenCard, TMP_Text messageText)
        {

            int value = selectionOfCards.OrderByDescending(c => c.totalCardValue).ToList().IndexOf(chosenCard);
            switch (value)
            {
                case 0:
                    player.points += 10;
                    messageText.text = $"{player.name} got the highest! (+10)";
                    break;
                case 1:
                    player.points += 3;
                    messageText.text = $"{player.name} got the middle! (+3)";
                    break;
                case 2:
                    player.points -= 3;
                    messageText.text = $"{player.name} got the lowest.. (-3)";
                    break;
            }

            messageText.text += $"\n{player.name} now has {player.points} points!";
        }
    }
}

    